using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Products.DTOs {
    public class AddProductVariantsRequestDto {
        [Required(ErrorMessage = "La lista de variantes no puede estar vacía.")]
        [MinLength(1, ErrorMessage = "Debes incluir al menos una variante.")]
        public ICollection<ProductVariantRequestDto> Variants { get; set; } = new List<ProductVariantRequestDto>();

    }
}
