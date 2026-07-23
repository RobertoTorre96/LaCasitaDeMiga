using System.Security.Claims;
using LaCasitaDeMiga.Features.Common.services.MailService;
using LaCasitaDeMiga.Features.Orders.DTOs;
using LaCasitaDeMiga.Features.Orders.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Orders {
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrderController : ControllerBase {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService) {
            _orderService = orderService;
        }

        [HttpPost]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] OrderRequestDto request) {
            if (!IsOwnerOrAdmin(request.CustomerId)) {
                return Forbid();
            }

            var response = await _orderService.CreateOrderAsync(request);
            await _orderService.SendOrderConfirmationEmailAsync(response);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpPost("combo-especial")]
        public async Task<ActionResult<OrderResponseDto>> CreateOrderComboEspecial([FromBody] ComboEspecialDTO request) {
            if (!IsOwnerOrAdmin(request.CustomerId)) {
                return Forbid();
            }

            var response = await _orderService.CreateOrderAsync(request);
            await _orderService.SendOrderConfirmationEmailAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrderResponseDto>> GetById(Guid id) {
            var response = await _orderService.GetByIdAsync(id);

            if (!IsOwnerOrAdmin(response.CustomerId)) {
                return Forbid();
            }

            return Ok(response);
        }

        [HttpGet("customer/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetByCustomerId(Guid customerId) {
            if (!IsOwnerOrAdmin(customerId)) {
                return Forbid();
            }

            var response = await _orderService.GetByCustomerIdAsync(customerId);
            return Ok(response);
        }

        [HttpPut("{id:guid}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<OrderResponseDto>> UpdateStatus(Guid id, [FromBody] EOrderStatus newStatus) {
            var response = await _orderService.UpdateStatusAsync(id, newStatus);
            return Ok(response);
        }

        // Verifica que el usuario logueado sea el dueño del recurso, o bien un Admin
        private bool IsOwnerOrAdmin(Guid resourceCustomerId) {
            if (User.IsInRole("Admin")) return true;

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return currentUserId == resourceCustomerId.ToString();
        }
    }
}