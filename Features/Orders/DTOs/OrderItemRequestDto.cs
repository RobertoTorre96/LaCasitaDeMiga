using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Orders.DTOs {
    public class OrderItemRequestDto {
        [Required(ErrorMessage = "El ID de la variante es obligatorio.")]
        public Guid ProductVariantId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria.")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos de 1 unidad.")]
        public int Quantity { get; set; }
    }
}
