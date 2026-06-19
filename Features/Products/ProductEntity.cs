using ECommersAPI.Features.Brands;
using ECommersAPI.Features.Categories;

namespace ECommersAPI.Features.Products {
    public class ProductEntity {

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Description { get; set; } = null!;
        public Guid CategoryId { get; set; }
        public Guid? BrandId { get; set; } // Opcional porque tu DDL permite NULL
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public CategoryEntity Category { get; set; } = null!;
        public BrandEntity? Brand { get; set; } // Opcional por el SET NULL


        public ICollection<ProductVariantEntity> Variants { get; set; } = new List<ProductVariantEntity>();


    }
}
