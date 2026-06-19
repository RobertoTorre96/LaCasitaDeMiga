using ECommersAPI.Features.Brands.DTOs;

namespace ECommersAPI.Features.Brands.Services {
    public interface IBrandService {
        Task<BrandResponseDto> CreateAsync(BrandRequestDto request);
        Task<IEnumerable<BrandResponseDto>> GetAllAsync();
        Task<BrandResponseDto> GetByIdAsync(Guid id);
        Task<BrandResponseDto> UpdateAsync(Guid id, BrandRequestDto request);
        Task DeleteAsync(Guid id);
    }
}
