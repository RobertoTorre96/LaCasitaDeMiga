using ECommersAPI.Features.Orders.DTOs;
using ECommersAPI.Features.Orders.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECommersAPI.Features.Orders {
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService) {
            _orderService = orderService;
        }

        // 1. ENDPOINT PARA CREAR LA ORDEN (CHECKOUT)
        // POST: api/order
        [HttpPost]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] OrderRequestDto request) {
            var response = await _orderService.CreateOrderAsync(request);
            // Devolvemos un 201 Created con la orden armada
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        // 2. ENDPOINT PARA OBTENER UNA ORDEN POR ID
        // GET: api/order/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrderResponseDto>> GetById(Guid id) {
            var response = await _orderService.GetByIdAsync(id);
            return Ok(response);
        }

        // 3. ENDPOINT PARA EL HISTORIAL DE UN CLIENTE
        // GET: api/order/customer/{customerId}
        [HttpGet("customer/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetByCustomerId(Guid customerId) {
            var response = await _orderService.GetByCustomerIdAsync(customerId);
            return Ok(response);
        }

        // 4. ENDPOINT PARA CAMBIAR EL ESTADO (ADMIN)
        // PUT: api/order/{id}/status
        [HttpPut("{id:guid}/status")]
        public async Task<ActionResult<OrderResponseDto>> UpdateStatus(Guid id, [FromBody] EOrderStatus newStatus) {
            var response = await _orderService.UpdateStatusAsync(id, newStatus);
            return Ok(response);
        }

    }
}
