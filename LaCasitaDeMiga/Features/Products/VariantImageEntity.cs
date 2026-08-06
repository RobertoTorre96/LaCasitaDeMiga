namespace LaCasitaDeMiga.Features.Products {
    public class VariantImageEntity {

        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid ProductVariantId { get; set; }
        public ProductVariantEntity ProductVariant { get; set; } = null!;

        public string ImageUrl { get; set; } = null!;
        public string PublicId { get; set; } = null!; // Para poder borrarla de Cloudinary

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
