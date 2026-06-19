namespace ECommersAPI.Features.Brands.DTOs {
    public class BrandResponseDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
