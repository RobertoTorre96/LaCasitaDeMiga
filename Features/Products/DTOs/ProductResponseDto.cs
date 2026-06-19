namespace ECommersAPI.Features.Products.DTOs {
    public class ProductResponseDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Datos limpios y rápidos para que el Frontend dibuje la procedencia del producto
        public ProductRelationDto Category { get; set; } = null!;
        public ProductRelationDto? Brand { get; set; } // Puede ser null si el producto no tiene marca

        // Lista de todas las variantes disponibles de este producto
        public ICollection<ProductVariantResponseDto> Variants { get; set; } = new List<ProductVariantResponseDto>();
    }

    // Un mini-DTO auxiliar reutilizable para no arrastrar toda la entidad Category o Brand pesada
    public class ProductRelationDto {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
    }
}
