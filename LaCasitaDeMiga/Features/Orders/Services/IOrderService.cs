using LaCasitaDeMiga.Features.Orders.DTOs;

namespace LaCasitaDeMiga.Features.Orders.Services {
    public interface IOrderService {
        Task<OrderResponseDto> CreateOrderAsync(OrderRequestDto request);
        Task<OrderResponseDto> CreateOrderAsync(ComboEspecialDTO request);

        Task<OrderResponseDto> GetByIdAsync(Guid id);

        Task<IEnumerable<OrderResponseDto>> GetByCustomerIdAsync(Guid customerId);

        Task<OrderResponseDto> UpdateStatusAsync(Guid orderId, EOrderStatus newStatus);
        Task SendOrderConfirmationEmailAsync(OrderResponseDto order);

        //borrar para github recluter
        Task SendOrderConfirmationEmailAsync(ComboEspecialDTO order); // ◄ ¡Faltaba esta línea!
    }
}
