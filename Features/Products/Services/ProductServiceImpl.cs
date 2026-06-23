using AutoMapper;
using LaCasitaDeMiga.Common.DTOs;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Products.DTOs;
using LaCasitaDeMiga.Data;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LaCasitaDeMiga.Features.Products.Services {
    public class ProductServiceImpl : IProductService {

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProductServiceImpl(ApplicationDbContext context, IMapper mapper) {
            _context = context;
            _mapper = mapper;
        }

        // 1. CREAR PRODUCTO (Con generación de SKU automática)
        public async Task<ProductResponseDto> CreateAsync(ProductRequestDto request) {
            var product = _mapper.Map<ProductEntity>(request);

            // Generamos el Slug único basado en el nombre
            product.Slug = GenerateSlug(request.Name);

            if (await _context.Products.AnyAsync(p => p.Slug == product.Slug)) {
                product.Slug = $"{product.Slug}-{Guid.NewGuid().ToString().Substring(0, 5)}";
            }

            // --- LÓGICA AUTOMÁTICA PARA EL SKU ---
            int variantIndex = 1;
            foreach (var variant in product.Variants) {
                // Buscamos si tiene algún atributo descriptivo (ej: Sabor, Talle, Color) para armar el SKU
                var firstAttributeValue = variant.Attributes.Values.FirstOrDefault()?.ToString() ?? "";
                string attributePart = !string.IsNullOrEmpty(firstAttributeValue)
                    ? $"-{GenerateSlug(firstAttributeValue)}"
                    : $"-v{variantIndex}";

                // Ej: "sandwich-miga-jamon-y-queso" o "vaso-vidrio-v1"
                variant.Sku = $"{product.Slug}{attributePart}".ToUpper();

                // Inicializamos los costos en 0 hasta que se registre la primera compra
                variant.LastPurchasePrice = 0.00m;
                variant.AverageCost = 0.00m;

                variantIndex++;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(product.Id);
        }

        // 2. REGISTRAR INGRESO DE MERCADERÍA (Fórmula de Costo Promedio Ponderado)
        public async Task<bool> RegisterStockEntryAsync(Guid variantId, int quantityReceived, decimal purchasePrice) {
            if (quantityReceived <= 0) {
                throw new BadRequestException("La cantidad recibida debe ser mayor a 0.");
            }
            if (purchasePrice < 0) {
                throw new BadRequestException("El precio de compra no puede ser negativo.");
            }

            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) throw new NotFoundException($"La variante con ID: '{variantId}' no fue encontrada.");

            int currentStock = variant.Stock;
            decimal currentAverageCost = variant.AverageCost;

            // Actualizamos siempre el último precio de compra del proveedor
            variant.LastPurchasePrice = purchasePrice;

            int newTotalStock = currentStock + quantityReceived;

            if (newTotalStock > 0) {
                // FÓRMULA: ((Stock Actual * Costo Promedio) + (Cantidad Nueva * Costo Nuevo)) / Stock Total
                decimal newAverageCost = ((currentStock * currentAverageCost) + (quantityReceived * purchasePrice)) / newTotalStock;
                variant.AverageCost = Math.Round(newAverageCost, 2);
            } else {
                // Si por alguna razón el stock era negativo o cero y se neutraliza
                variant.AverageCost = purchasePrice;
            }

            // Sumamos el nuevo stock físico
            variant.Stock = newTotalStock;

            return await _context.SaveChangesAsync() > 0;
        }

        // 3. OBTENER TODOS PAGINADOS
        public async Task<PagedResultDto<ProductResponseDto>> GetAllAsync(
                                                                    Guid? categoryId = null,
                                                                    Guid? brandId = null,
                                                                    bool onlyActive = true,
                                                                    int pageNumber = 1,
                                                                    int pageSize = 10) {

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .AsQueryable();

            if (onlyActive) query = query.Where(p => p.IsActive);
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId.Value);

            var totalItems = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var mappedItems = _mapper.Map<IEnumerable<ProductResponseDto>>(products);

            return new PagedResultDto<ProductResponseDto> {
                Items = mappedItems,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        // 4. OBTENER POR ID
        public async Task<ProductResponseDto?> GetByIdAsync(Guid id) {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) throw new NotFoundException("Producto no encontrado");
            return _mapper.Map<ProductResponseDto>(product);
        }

        // 5. OBTENER POR SLUG
        public async Task<ProductResponseDto?> GetBySlugAsync(string slug) {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (product == null) throw new NotFoundException("Producto no encontrado");
            return _mapper.Map<ProductResponseDto>(product);
        }

        // 6. ACTUALIZAR DETALLES
        public async Task<ProductResponseDto?> UpdateAsync(Guid id, ProductRequestDto request) {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) throw new NotFoundException($"El Producto con Id: '{id}' no se ha encontrado.");
            _mapper.Map(request, product);

            product.UpdatedAt = DateTime.UtcNow;
            product.Slug = GenerateSlug(request.Name);

            await _context.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        // 7. ACTUALIZAR STOCK MANUAL (Se mantiene para ajustes/ventas de caja)
        public async Task<bool> UpdateStockAsync(Guid variantId, int quantity) {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) throw new NotFoundException($"La variante con ID: '{variantId}' no fue encontrada");

            if (quantity < 0 && (variant.Stock + quantity) < 0) {
                throw new BadRequestException(
                    $"No hay stock suficiente para el SKU {variant.Sku}. " +
                    $"Stock disponible: {variant.Stock}, solicitado: {Math.Abs(quantity)}."
                );
            }
            variant.Stock += quantity;
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdatePricesAsync(Guid variantId, UpdatePricesRequestDto dto) {
            if (dto.Price <= 0) {
                throw new BadRequestException("El precio de venta debe ser mayor a 0.");
            }

            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) throw new NotFoundException($"La variante con ID: '{variantId}' no fue encontrada.");

            // Actualizamos los precios de venta
            variant.Price = dto.Price;
            variant.CompareAtPrice = dto.CompareAtPrice;

            return await _context.SaveChangesAsync() > 0;
        }


        // 8. ELIMINAR
        public async Task<bool> DeleteAsync(Guid id) {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) throw new NotFoundException($"El Producto con Id: '{id}' no se ha encontrado.");

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();
            return true;
        }

        private string GenerateSlug(string phrase) {
            string str = phrase.ToLower();
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }
    }
}