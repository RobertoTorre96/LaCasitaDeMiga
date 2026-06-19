using System.ComponentModel.DataAnnotations;

namespace ECommersAPI.Features.Orders.DTOs {
    public class OrderRequestDto {
        [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
        public Guid CustomerId { get; set; }

        [Required(ErrorMessage = "La orden debe contener al menos un producto.")]
        [MinLength(1, ErrorMessage = "El carrito no puede estar vacío.")]
        public List<OrderItemRequestDto> Items { get; set; } = new();
    }
}
