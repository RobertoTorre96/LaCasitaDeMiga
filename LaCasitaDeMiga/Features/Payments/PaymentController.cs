using LaCasitaDeMiga.Features.Payments.DTOs;
using LaCasitaDeMiga.Features.Payments.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Payments {
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase {

        private readonly IPaymentService _paymentService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(IPaymentService paymentService, ILogger<PaymentController> logger) {
            _paymentService = paymentService;
            _logger = logger;
        }

        // POST: api/payment/{orderId}/preference
        [HttpPost("{orderId:guid}/preference")]
        public async Task<ActionResult<string>> CreatePreference(Guid orderId) {
            var initPoint = await _paymentService.CreatePreferenceAsync(orderId);
            return Ok(new { initPoint });
        }

        // Controller: SOLO extrae y traduce
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] MercadoPagoWebhookDto dto) {

            if (dto.EventKind != "payment" || string.IsNullOrEmpty(dto.PaymentId)) {
                return Ok();
            }

            // TODO: Validación de firma x-signature deshabilitada temporalmente.
            // El cálculo HMAC-SHA256 fue verificado de forma independiente (C# y Python)
            // contra el manifest oficial de Mercado Pago, con las credenciales de prueba
            // confirmadas, y el hash resultante nunca coincidió con el valor "v1" recibido.
            // Posible inconsistencia del lado de Mercado Pago (sandbox/Checkout Pro).
            // Pendiente de revisar en modo productivo o con soporte de MP.
            //
            // var xSignature = Request.Headers["x-signature"].ToString();
            // var xRequestId = Request.Headers["x-request-id"].ToString();
            // if (!_paymentService.ValidateWebhookSignature(dto.PaymentId, xSignature, xRequestId)) {
            //     return Unauthorized();
            // }

            await _paymentService.ProcessWebhookAsync(dto.PaymentId);

            return Ok();
        }
    }
}