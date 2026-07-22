using LaCasitaDeMiga.Features.Common.services.MailService;
using LaCasitaDeMiga.Features.Orders.DTOs;
using LaCasitaDeMiga.Features.Orders.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Orders {
    [ApiController]
    [Route("api/orders")]
    public class OrderController : ControllerBase {
        private readonly IOrderService _orderService;

        // Limpiamos el parámetro "email" del constructor ya que no se estaba usando
        public OrderController(IOrderService orderService) {
            _orderService = orderService;
        }

        // 1. ENDPOINT PARA CREAR LA ORDEN (CHECKOUT REGULAR)
        // POST: api/order
        [HttpPost]
        public async Task<ActionResult<OrderResponseDto>> CreateOrder([FromBody] OrderRequestDto request) {
            var response = await _orderService.CreateOrderAsync(request);
            await _orderService.SendOrderConfirmationEmailAsync(response);

            // Devolvemos un 201 Created con la ruta hacia el GetById correspondiente
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        //borrar para github recluter
        // POST: api/order/combo-especial
        [HttpPost("combo-especial")] // ◄ ¡Corregido! Ruta única para Swagger
        public async Task<ActionResult<OrderResponseDto>> CreateOrderComboEspecial([FromBody] ComboEspecialDTO request) {
            // ◄ Guardamos la respuesta real del servicio (OrderResponseDto)
            var response = await _orderService.CreateOrderAsync(request);

            // ◄ Enviamos el correo usando el DTO del combo que tiene los datos desglosados
            await _orderService.SendOrderConfirmationEmailAsync(request);

            // Devolvemos un 201 Created apuntando al ID real de la orden creada
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }


        // 3. ENDPOINT PARA OBTENER UNA ORDEN POR ID
        // GET: api/order/{id}
        [HttpGet("{id:guid}")]
        public async Task<ActionResult<OrderResponseDto>> GetById(Guid id) {
            var response = await _orderService.GetByIdAsync(id);
            return Ok(response);
        }

        // 4. ENDPOINT PARA EL HISTORIAL DE UN CLIENTE
        // GET: api/order/customer/{customerId}
        [HttpGet("customer/{customerId:guid}")]
        public async Task<ActionResult<IEnumerable<OrderResponseDto>>> GetByCustomerId(Guid customerId) {
            var response = await _orderService.GetByCustomerIdAsync(customerId);
            return Ok(response);
        }

        // 5. ENDPOINT PARA CAMBIAR EL ESTADO (ADMIN)
        // PUT: api/order/{id}/status
        [HttpPut("{id:guid}/status")]
        public async Task<ActionResult<OrderResponseDto>> UpdateStatus(Guid id, [FromBody] EOrderStatus newStatus) {
            var response = await _orderService.UpdateStatusAsync(id, newStatus);
            return Ok(response);
        }
    }
} 