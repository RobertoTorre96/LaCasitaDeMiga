using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Products.DTOs {
    public class ProductUpdateDto {
        [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
        [StringLength(150, ErrorMessage = "El nombre no puede superar los 150 caracteres.")]
        public string Name { get; set; } = null!;

        [Required(ErrorMessage = "La descripción es obligatoria.")]
        public string Description { get; set; } = null!;

        [Required(ErrorMessage = "La categoría es obligatoria.")]
        public Guid CategoryId { get; set; }

        public Guid? BrandId { get; set; }
    }
}
