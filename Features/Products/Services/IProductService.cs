using ECommersAPI.Common.DTOs;
using ECommersAPI.Features.Products.DTOs;

namespace ECommersAPI.Features.Products.Services {
    public interface IProductService {
        Task<PagedResultDto<ProductResponseDto>> GetAllAsync(
            Guid? categoryId = null,
            Guid? brandId = null,
            bool onlyActive = true,
            int pageNumber = 1,
            int pageSize = 10);
        Task<ProductResponseDto> CreateAsync(ProductRequestDto request);
        Task<ProductResponseDto?> GetByIdAsync(Guid id);
        Task<ProductResponseDto?> GetBySlugAsync(string slug);


        Task<ProductResponseDto?> UpdateAsync(Guid id, ProductRequestDto request);
        Task<bool> UpdateStockAsync(Guid variantId, int quantity);

        Task<bool> DeleteAsync(Guid id);



    }
}
