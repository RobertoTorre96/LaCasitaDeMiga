using LaCasitaDeMiga.Features.Categories.DTOs;

namespace LaCasitaDeMiga.Features.Categories.Services {
    public interface ICategoryService {
        Task<IEnumerable<CategoryResponseDto>> GetAllAsync();

        Task<CategoryResponseDto> CreateAsync(CategoryRequestDto request);
        Task<CategoryResponseDto> GetByIdAsync(Guid id);
        Task<CategoryResponseDto> UpdateAsync(Guid id, CategoryRequestDto request);
        Task DeleteAsync(Guid id);
    }
}

