using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Delivery.DTOs {
    public class DeliveryLookupRequestDto {
        [Required(ErrorMessage = "La dirección es obligatoria.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "La dirección debe tener entre 5 y 200 caracteres.")]
        public string Address { get; set; } = null!;
    }
}
