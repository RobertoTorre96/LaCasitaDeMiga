using LaCasitaDeMiga.Common.DTOs;
using LaCasitaDeMiga.Features.Orders.DTOs;

namespace LaCasitaDeMiga.Features.Orders.Services {
    public interface IOrderService {
        Task<OrderResponseDto> CreateOrderAsync(OrderRequestDto request);
        Task<OrderResponseDto> CreateOrderAsync(ComboEspecialDTO request);
        Task<OrderResponseDto> CreateOrderWithoutStockAsync(OrderRequestDto request);

        Task<OrderResponseDto> GetByIdAsync(Guid id);

        Task<IEnumerable<OrderResponseDto>> GetByCustomerIdAsync(Guid customerId);

        Task<OrderResponseDto> UpdateStatusAsync(Guid orderId, EOrderStatus newStatus);
        Task SendOrderConfirmationEmailAsync(OrderResponseDto order);
        Task<PagedResultDto<OrderResponseDto>> GetAllAsync(
                                                           EOrderStatus? status = null,
                                                           DateTime? startDate = null,
                                                           DateTime? endDate = null,
                                                           int pageNumber = 1,
                                                           int pageSize = 10);



        Task SendOrderConfirmationEmailAsync(ComboEspecialDTO order); 

    }
}
