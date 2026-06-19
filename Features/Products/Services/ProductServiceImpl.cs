using AutoMapper;
using ECommerceAPI.Data;
using ECommersAPI.Common.DTOs;
using ECommersAPI.Exceptions;
using ECommersAPI.Features.Products.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ECommersAPI.Features.Products.Services {
    public class ProductServiceImpl : IProductService {

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public ProductServiceImpl(ApplicationDbContext context, IMapper mapper) {
            _context = context;
            _mapper = mapper;
        }
        public async Task<ProductResponseDto> CreateAsync(ProductRequestDto request) {
            var product = _mapper.Map<ProductEntity>(request);

            // 2. Generamos el Slug único basado en el nombre
            product.Slug = GenerateSlug(request.Name);

            // 3. Validar si el slug ya existe (opcional pero recomendado)
            if (await _context.Products.AnyAsync(p => p.Slug == product.Slug)) {
                product.Slug = $"{product.Slug}-{Guid.NewGuid().ToString().Substring(0, 5)}";
            }

            // 4. Guardar en BD (El SaveChanges guardará padre e hijos de un tirón)
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // 5. Devolver el producto creado cargando sus relaciones para el Response
            return await GetByIdAsync(product.Id);
        }



        public async Task<PagedResultDto<ProductResponseDto>> GetAllAsync(
                                                                    Guid? categoryId = null,
                                                                    Guid? brandId = null,
                                                                    bool onlyActive = true,
                                                                    int pageNumber = 1,
                                                                    int pageSize = 10) {

            // 1. Validamos que no nos manden páginas o tamaños locos
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50; // Ponemos un tope para que no nos rompan la API pidiendo 1 millón de registros

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .AsQueryable();

            // 2. Aplicamos filtros dinámicos (Esto se ejecuta en la BD)
            if (onlyActive) query = query.Where(p => p.IsActive);
            if (categoryId.HasValue) query = query.Where(p => p.CategoryId == categoryId.Value);
            if (brandId.HasValue) query = query.Where(p => p.BrandId == brandId.Value);

            // 3. Contamos el TOTAL de elementos CON los filtros aplicados (Vital para el Frontend)
            var totalItems = await query.CountAsync();

            // 4. LA PAGINACIÓN: Aplicamos Skip y Take antes de ir a buscar la lista final
            var products = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 5. Mapeamos los productos de esta página a DTOs
            var mappedItems = _mapper.Map<IEnumerable<ProductResponseDto>>(products);

            // 6. Armamos el resultado empaquetado
            return new PagedResultDto<ProductResponseDto> {
                Items = mappedItems,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        public async Task<ProductResponseDto?> GetByIdAsync(Guid id) {

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product== null) throw new NotFoundException("Producto no encontrado");
            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<ProductResponseDto?> GetBySlugAsync(string slug) {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (product == null) throw new NotFoundException("Producto no encontrado");
            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task<ProductResponseDto?> UpdateAsync(Guid id, ProductRequestDto request) {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) throw new NotFoundException($"El Producto con Id: '{id}' no se ah encontrado.");
            _mapper.Map(request, product);

            // Actualizamos las propiedades de auditoría y SEO
            product.UpdatedAt = DateTime.UtcNow;
            product.Slug = GenerateSlug(request.Name);

            // Ahora sí, guardamos los cambios reales en la BD
            await _context.SaveChangesAsync();

            // Devolvemos el producto fresco usando GetById para rehidratar todas las relaciones
            return await GetByIdAsync(id);
        }

        public async Task<bool> UpdateStockAsync(Guid variantId, int quantity) {
            var variant = await _context.ProductVariants.FindAsync(variantId);
            if (variant == null) throw new NotFoundException($"La variante con ID: '{variantId}' no fue encontrado");

            if (quantity < 0 && (variant.Stock + quantity) < 0) {
                throw new BadRequestException(
                    $"No hay stock suficiente para el SKU {variant.Sku}. " +
                    $"Stock disponible: {variant.Stock}, solicitado: {Math.Abs(quantity)}."
                );
            }
            variant.Stock += quantity;
            return await _context.SaveChangesAsync() > 0;
        }
        public async Task<bool> DeleteAsync(Guid id) {
            var product = await _context.Products
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) throw new NotFoundException($"El Producto con Id: '{id}' no se ah encontrado.");

            _context.Products.Remove(product);
          await _context.SaveChangesAsync();
            return true;

        }





        private string GenerateSlug(string phrase) {
            string str = phrase.ToLower();
            str = Regex.Replace(str, @"[^a-z0-9\s-]", ""); // Quita caracteres especiales
            str = Regex.Replace(str, @"\s+", " ").Trim(); // Quita espacios extra
            str = Regex.Replace(str, @"\s", "-"); // Espacios por guiones
            return str;
        }
    }
}
