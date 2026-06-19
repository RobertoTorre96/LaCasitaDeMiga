using System.ComponentModel.DataAnnotations;

namespace ECommersAPI.Features.Products.DTOs {
    public class ProductRequestDto {
        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "La descripción es obligatorio.")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        public Guid CategoryId { get; set; }

        // Opcional, ya que tu DDL permite que un producto no tenga marca (NULL)
        public Guid? BrandId { get; set; }

        // Lista de variantes obligatoria: un producto genérico debe nacer con al menos una variante (ej: la variante estándar)
        [Required(ErrorMessage = "El producto debe tener al menos una variante.")]
        public ICollection<ProductVariantRequestDto> Variants { get; set; } = new List<ProductVariantRequestDto>();
    }
}
