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
            this._context = context;
            this._mapper = mapper;
        }

        // 1. CREAR PRODUCTO (Con generación de SKU y Priority automática/opcional)
        public async Task<ProductResponseDto> CreateAsync(ProducCreatetRequestDto request) {
            var product = _mapper.Map<ProductEntity>(request);

            // El Slug siempre se genera de forma automática basado en el nombre base
            product.Slug = GenerateSlug(request.Name);

            if (await _context.Products.AnyAsync(p => p.Slug == product.Slug)) {
                product.Slug = $"{product.Slug}-{Guid.NewGuid().ToString().Substring(0, 5)}";
            }

            // Calculamos la prioridad máxima actual de la base de datos por si se necesita asignar
            int maxPriority = await _context.ProductVariants
                .Select(v => (int?)v.Priority)
                .MaxAsync() ?? 0;

            // --- LÓGICA PARA EL SKU (OPCIONAL) Y PRIORIDADES ---
            int variantIndex = 1;
            foreach (var variant in product.Variants) {

                // Si el administrador envió un SKU manual en el DTO, lo usamos.
                if (!string.IsNullOrWhiteSpace(variant.Sku)) {
                    variant.Sku = variant.Sku.Trim().ToUpper();
                } else {
                    var firstAttributeValue = variant.Attributes.Values.FirstOrDefault()?.ToString() ?? "";
                    string attributePart = !string.IsNullOrEmpty(firstAttributeValue)
                        ? $"-{GenerateSlug(firstAttributeValue)}"
                        : $"-v{variantIndex}";

                    variant.Sku = $"{product.Slug}{attributePart}".ToUpper();
                }

                // La versión inicial obligatoria para concurrencia
                variant.Version = 1;

                // Resolución de prioridades automáticas si vienen en 0
                if (variant.Priority == 0) {
                    maxPriority++;
                    variant.Priority = maxPriority;
                }

                variantIndex++;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            return await GetByIdAsync(product.Id);
        }

        // 2. REGISTRAR INGRESO DE MERCADERÍA (Fórmula de Costo Promedio Ponderado + Concurrencia)
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

            variant.LastPurchasePrice = purchasePrice;
            int newTotalStock = currentStock + quantityReceived;

            if (newTotalStock > 0) {
                decimal newAverageCost = ((currentStock * currentAverageCost) + (quantityReceived * purchasePrice)) / newTotalStock;
                variant.AverageCost = Math.Round(newAverageCost, 2);
            } else {
                variant.AverageCost = purchasePrice;
            }

            variant.Stock = newTotalStock;

            // --- INCREMENTO SEGURO DE CONCURRENCIA ---
            variant.Version++;

            try {
                return await _context.SaveChangesAsync() > 0;
            } catch (DbUpdateConcurrencyException) {
                throw new ConflictException("No se pudo registrar el ingreso. La variante fue modificada en simultáneo por otro proceso.");
            }
        }

        // 3. OBTENER TODOS PAGINADOS (Ordenados por Prioridad de variante)
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

            // Ordenamos las variantes por prioridad antes de mapear al DTO
            product.Variants = product.Variants.OrderByDescending(v => v.Priority).ToList();

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

            product.Variants = product.Variants.OrderByDescending(v => v.Priority).ToList();

            return _mapper.Map<ProductResponseDto>(product);
        }

        // 6. ACTUALIZAR DETALLES
        public async Task<ProductResponseDto?> UpdateAsync(Guid id, ProductUpdateDto request) {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) throw new NotFoundException($"El Producto con Id: '{id}' no se ha encontrado.");

            product.Name = request.Name;
            product.Description = request.Description;
            product.CategoryId = request.CategoryId;
            product.BrandId = request.BrandId;
            product.UpdatedAt = DateTime.UtcNow;
            product.Slug = GenerateSlug(request.Name);

            int variantIndex = 1;
            foreach (var variant in product.Variants) {
                var firstAttributeValue = variant.Attributes.Values.FirstOrDefault()?.ToString() ?? "";
                string attributePart = !string.IsNullOrEmpty(firstAttributeValue)
                    ? $"-{GenerateSlug(firstAttributeValue)}"
                    : $"-v{variantIndex}";

                variant.Sku = $"{product.Slug}{attributePart}".ToUpper();
                variantIndex++;
            }

            await _context.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        // 7. ACTUALIZAR STOCK MANUAL (Ajustes o ventas de caja resguardados)
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

            // --- INCREMENTO SEGURO DE CONCURRENCIA ---
            variant.Version++;

            try {
                return await _context.SaveChangesAsync() > 0;
            } catch (DbUpdateConcurrencyException) {
                throw new ConflictException("No se pudo modificar el stock. El producto está siendo afectado por otra transacción simultánea.");
            }
        }

        // 8. ACTUALIZAR VARIANTE (Edición del Panel Admin)
        public async Task<ProductVariantResponseDto> UpdateVariantAsync(Guid variantId, UpdateProductVariantRequestDto dto) {
            var variant = await _context.ProductVariants
                .Include(v => v.Product)
                .FirstOrDefaultAsync(v => v.Id == variantId);

            if (variant == null) {
                throw new NotFoundException($"La variante con ID: '{variantId}' no fue encontrada.");
            }

            // Aplicamos los cambios directo del mapeo manual
            variant.Price = dto.Price;
            variant.CompareAtPrice = dto.CompareAtPrice;
            variant.LowStockThreshold = dto.LowStockThreshold;
            variant.Attributes = dto.Attributes;
            variant.IsActive = dto.IsActive;

            // --- RESOLUCIÓN DE REGLAS DE NEGOCIO NUEVAS PARA ACTUALIZACIÓN ---
            variant.IsFeatured = dto.IsFeatured ?? false;

            if (dto.Priority.HasValue && dto.Priority.Value > 0) {
                variant.Priority = dto.Priority.Value;
            } else if (variant.Priority == 0) {
                int maxPriority = await _context.ProductVariants.Select(v => (int?)v.Priority).MaxAsync() ?? 0;
                variant.Priority = maxPriority + 1;
            }

            // VALIDACIÓN DE SKU OPCIONAL EN ACTUALIZACIÓN
            if (!string.IsNullOrWhiteSpace(dto.Sku)) {
                variant.Sku = dto.Sku.Trim().ToUpper();
            } else {
                var firstAttributeValue = variant.Attributes.Values.FirstOrDefault()?.ToString() ?? "";
                string attributePart = !string.IsNullOrEmpty(firstAttributeValue)
                    ? $"-{GenerateSlug(firstAttributeValue)}"
                    : $"-v1";

                variant.Sku = $"{variant.Product.Slug}{attributePart}".ToUpper();
            }

            // --- INCREMENTO SEGURO DE CONCURRENCIA ---
            variant.Version++;

            try {
                await _context.SaveChangesAsync();
                return _mapper.Map<ProductVariantResponseDto>(variant);
            } catch (DbUpdateConcurrencyException) {
                throw new ConflictException("El formulario de edición falló porque otro usuario guardó cambios en esta variante recientemente.");
            }
        }
        // 10. AGREGAR VARIANTES A UN PRODUCTO EXISTENTE (Respetando PurchasePrice del DTO)
        public async Task<ProductResponseDto> AddVariantsAsync(Guid productId, AddProductVariantsRequestDto request) {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) {
                throw new NotFoundException($"El Producto con Id: '{productId}' no existe.");
            }

            int? cachedMaxPriority = null;
            int variantIndex = product.Variants.Count + 1;

            foreach (var variantDto in request.Variants) {
                // AutoMapper ya pasa automáticamente 'AverageCost' y 'LastPurchasePrice' mapeados desde 'PurchasePrice'
                var newVariant = _mapper.Map<ProductVariantEntity>(variantDto);

                newVariant.ProductId = productId;

                // --- LÓGICA DE SKU ---
                if (!string.IsNullOrWhiteSpace(newVariant.Sku)) {
                    newVariant.Sku = newVariant.Sku.Trim().ToUpper();
                } else {
                    var firstAttributeValue = newVariant.Attributes.Values.FirstOrDefault()?.ToString() ?? "";
                    string attributePart = !string.IsNullOrEmpty(firstAttributeValue)
                        ? $"-{GenerateSlug(firstAttributeValue)}"
                        : $"-v{variantIndex}";

                    newVariant.Sku = $"{product.Slug}{attributePart}".ToUpper();
                }

                if (await _context.ProductVariants.AnyAsync(v => v.Sku == newVariant.Sku)) {
                    throw new BadRequestException($"El SKU '{newVariant.Sku}' ya está registrado en otra variante.");
                }

                newVariant.Version = 1;
                newVariant.IsActive = true;

                // --- LÓGICA DE PRIORIDAD ---
                if (newVariant.Priority == 0) {
                    if (cachedMaxPriority == null) {
                        cachedMaxPriority = await _context.ProductVariants
                            .Select(v => (int?)v.Priority)
                            .MaxAsync() ?? 0;
                    }

                    cachedMaxPriority++;
                    newVariant.Priority = cachedMaxPriority.Value;
                }

                _context.ProductVariants.Add(newVariant);
                variantIndex++;
            }

            await _context.SaveChangesAsync();
            return await GetByIdAsync(productId)!;
        }

        // 9. ELIMINAR
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