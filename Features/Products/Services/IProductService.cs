using LaCasitaDeMiga.Common.DTOs;
using LaCasitaDeMiga.Features.Products.DTOs;

namespace LaCasitaDeMiga.Features.Products.Services {
    public interface IProductService {
        Task<PagedResultDto<ProductResponseDto>> GetAllAsync(
            Guid? categoryId = null,
            Guid? brandId = null,
            bool onlyActive = true,
            int pageNumber = 1,
            int pageSize = 10);

        Task<ProductResponseDto> CreateAsync(ProducCreatetRequestDto request);
        Task<ProductResponseDto?> GetByIdAsync(Guid id);
        Task<ProductResponseDto?> GetBySlugAsync(string slug);
        Task<ProductResponseDto?> UpdateAsync(Guid id, ProductUpdateDto request);

        // Se mantiene para el egreso por ventas o ajustes directos de stock
        Task<bool> UpdateStockAsync(Guid variantId, int quantity);

        // --- NUEVO MÉTODO DECLARADO ---
        // Se usa para el ingreso de mercadería de proveedores y recalcular el costo promedio ponderado
        Task<bool> RegisterStockEntryAsync(Guid variantId, int quantityReceived, decimal purchasePrice);
        // ──────────────────────────────
        Task<ProductVariantResponseDto> UpdateVariantAsync(Guid variantId, UpdateProductVariantRequestDto dto);

        Task<bool> DeleteAsync(Guid id);
    }
}