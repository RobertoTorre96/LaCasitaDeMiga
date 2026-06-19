namespace ECommersAPI.Features.Categories.DTOs {
    public class CategoryResponseDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
