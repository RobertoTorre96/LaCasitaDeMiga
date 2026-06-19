using ECommersAPI.Features.Orders.DTOs;

namespace ECommersAPI.Features.Orders.Services {
    public interface IOrderService {
        Task<OrderResponseDto> CreateOrderAsync(OrderRequestDto request);

        Task<OrderResponseDto> GetByIdAsync(Guid id);

        Task<IEnumerable<OrderResponseDto>> GetByCustomerIdAsync(Guid customerId);

        Task<OrderResponseDto> UpdateStatusAsync(Guid orderId, EOrderStatus newStatus);
    }
}
