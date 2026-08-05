using AutoMapper;
using LaCasitaDeMiga.Common.DTOs;
using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Common.Cache.services;
using LaCasitaDeMiga.Features.Products.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace LaCasitaDeMiga.Features.Products.Services {
    public class ProductServiceImpl : IProductService {

        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;

        // --- CONFIGURACIÓN DE CACHÉ ---
        private static readonly TimeSpan IndividualCacheTtl = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan ListCacheTtl = TimeSpan.FromSeconds(60);
        private const string ListVersionKey = "products:version";

        public ProductServiceImpl(ApplicationDbContext context, IMapper mapper, ICacheService cache) {
            this._context = context;
            this._mapper = mapper;
            this._cache = cache;
        }

        // 1. CREAR PRODUCTO (Con generación de SKU y Priority automática/opcional)
        public async Task<ProductResponseDto> CreateAsync(ProducCreatetRequestDto request) {
            var product = _mapper.Map<ProductEntity>(request);

            product.Slug = GenerateSlug(request.Name);

            if (await _context.Products.AnyAsync(p => p.Slug == product.Slug)) {
                product.Slug = $"{product.Slug}-{Guid.NewGuid().ToString().Substring(0, 5)}";
            }

            int maxPriority = await _context.ProductVariants
                .Select(v => (int?)v.Priority)
                .MaxAsync() ?? 0;

            int variantIndex = 1;
            foreach (var variant in product.Variants) {
                if (!string.IsNullOrWhiteSpace(variant.Sku)) {
                    variant.Sku = variant.Sku.Trim().ToUpper();
                } else {
                    var firstAttributeValue = variant.Attributes.Values.FirstOrDefault()?.ToString() ?? "";
                    string attributePart = !string.IsNullOrEmpty(firstAttributeValue)
                        ? $"-{GenerateSlug(firstAttributeValue)}"
                        : $"-v{variantIndex}";

                    variant.Sku = $"{product.Slug}{attributePart}".ToUpper();
                }

                variant.Version = 1;

                if (variant.Priority == 0) {
                    maxPriority++;
                    variant.Priority = maxPriority;
                }

                variantIndex++;
            }

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            await InvalidateListCacheAsync();

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
            variant.Version++;

            try {
                var result = await _context.SaveChangesAsync() > 0;

                // Invalidamos el producto por ID. El de slug se autocorrige por TTL corto (ver nota arriba).
                await InvalidateProductCacheAsync(variant.ProductId);
                await InvalidateListCacheAsync();

                return result;
            } catch (DbUpdateConcurrencyException) {
                throw new ConflictException("No se pudo registrar el ingreso. La variante fue modificada en simultáneo por otro proceso.");
            }
        }

        // 3. OBTENER TODOS PAGINADOS (Ordenados por Prioridad de variante) — CACHEADO
        public async Task<PagedResultDto<ProductResponseDto>> GetAllAsync(
                                                                    Guid? categoryId = null,
                                                                    Guid? brandId = null,
                                                                    bool onlyActive = true,
                                                                    int pageNumber = 1,
                                                                    int pageSize = 10) {

            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 50) pageSize = 50;

            // La clave incluye la "versión" actual: si algo cambió, la versión sube
            // y esta combinación de filtros deja de coincidir con ninguna clave vieja.
            var version = await _cache.GetVersionAsync(ListVersionKey);
            var cacheKey = $"products:list:v{version}:cat={categoryId}:brand={brandId}:active={onlyActive}:page={pageNumber}:size={pageSize}";

            var cached = await _cache.GetAsync<PagedResultDto<ProductResponseDto>>(cacheKey);
            if (cached != null) return cached;

            var query = _context.Products.AsNoTracking()
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

            var result = new PagedResultDto<ProductResponseDto> {
                Items = mappedItems,
                TotalItems = totalItems,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            await _cache.SetAsync(cacheKey, result, ListCacheTtl);

            return result;
        }

        // 4. OBTENER POR ID — CACHEADO
        public async Task<ProductResponseDto?> GetByIdAsync(Guid id) {
            var cacheKey = $"products:id:{id}";

            var cached = await _cache.GetAsync<ProductResponseDto>(cacheKey);
            if (cached != null) return cached;

            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) throw new NotFoundException("Producto no encontrado");

            product.Variants = product.Variants.OrderByDescending(v => v.Priority).ToList();

            var dto = _mapper.Map<ProductResponseDto>(product);

            await _cache.SetAsync(cacheKey, dto, IndividualCacheTtl);

            return dto;
        }

        // 5. OBTENER POR SLUG — CACHEADO
        public async Task<ProductResponseDto?> GetBySlugAsync(string slug) {
            var cacheKey = $"products:slug:{slug}";

            var cached = await _cache.GetAsync<ProductResponseDto>(cacheKey);
            if (cached != null) return cached;

            var product = await _context.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Variants)
                .FirstOrDefaultAsync(p => p.Slug == slug);

            if (product == null) throw new NotFoundException("Producto no encontrado");

            product.Variants = product.Variants.OrderByDescending(v => v.Priority).ToList();

            var dto = _mapper.Map<ProductResponseDto>(product);

            await _cache.SetAsync(cacheKey, dto, IndividualCacheTtl);

            return dto;
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
            var oldSlug = product.Slug;
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

            await InvalidateProductCacheAsync(id, oldSlug);
            if (oldSlug != product.Slug) {
                await _cache.RemoveAsync($"products:slug:{product.Slug}");
            }
            await InvalidateListCacheAsync();

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
            variant.Version++;

            try {
                var result = await _context.SaveChangesAsync() > 0;

                await InvalidateProductCacheAsync(variant.ProductId);
                await InvalidateListCacheAsync();

                return result;
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

            variant.Price = dto.Price;
            variant.CompareAtPrice = dto.CompareAtPrice;
            variant.LastPurchasePrice = dto.LastPurchasePrice;
            variant.LowStockThreshold = dto.LowStockThreshold;
            variant.Attributes = dto.Attributes;
            variant.IsActive = dto.IsActive;
            variant.IsFeatured = dto.IsFeatured ?? false;

            if (dto.Priority.HasValue && dto.Priority.Value > 0) {
                variant.Priority = dto.Priority.Value;
            } else if (variant.Priority == 0) {
                int maxPriority = await _context.ProductVariants.Select(v => (int?)v.Priority).MaxAsync() ?? 0;
                variant.Priority = maxPriority + 1;
            }

            if (!string.IsNullOrWhiteSpace(dto.Sku)) {
                variant.Sku = dto.Sku.Trim().ToUpper();
            } else {
                var firstAttributeValue = variant.Attributes.Values.FirstOrDefault()?.ToString() ?? "";
                string attributePart = !string.IsNullOrEmpty(firstAttributeValue)
                    ? $"-{GenerateSlug(firstAttributeValue)}"
                    : $"-v1";

                variant.Sku = $"{variant.Product.Slug}{attributePart}".ToUpper();
            }

            variant.Version++;

            try {
                await _context.SaveChangesAsync();

                await InvalidateProductCacheAsync(variant.ProductId, variant.Product.Slug);
                await InvalidateListCacheAsync();

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
                var newVariant = _mapper.Map<ProductVariantEntity>(variantDto);

                newVariant.ProductId = productId;

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

            await InvalidateProductCacheAsync(productId, product.Slug);
            await InvalidateListCacheAsync();

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

            await InvalidateProductCacheAsync(id, product.Slug);
            await InvalidateListCacheAsync();

            return true;
        }

        private string GenerateSlug(string phrase) {
            string str = phrase.ToLower();
            str = Regex.Replace(str, @"[^a-z0-9\s-]", "");
            str = Regex.Replace(str, @"\s+", " ").Trim();
            str = Regex.Replace(str, @"\s", "-");
            return str;
        }

        // --- HELPERS DE INVALIDACIÓN DE CACHÉ ---

        private async Task InvalidateProductCacheAsync(Guid productId, string? slug = null) {
            await _cache.RemoveAsync($"products:id:{productId}");
            if (!string.IsNullOrEmpty(slug)) {
                await _cache.RemoveAsync($"products:slug:{slug}");
            }
            // Nota: en UpdateStockAsync y RegisterStockEntryAsync no tenemos el slug disponible
            // sin una consulta extra a la base. Ese caché puntual se autocorrige solo por su TTL corto (2 min).
        }

        private async Task InvalidateListCacheAsync() {
            await _cache.IncrementVersionAsync(ListVersionKey);
        }
    }
}