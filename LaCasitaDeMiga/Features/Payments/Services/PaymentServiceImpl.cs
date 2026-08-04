using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Orders;
using LaCasitaDeMiga.Features.Orders.Services;
using MercadoPago.Client.Payment;
using MercadoPago.Client.Preference;
using MercadoPago.Error;
using MercadoPago.Resource.Payment;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace LaCasitaDeMiga.Features.Payments.Services {
    public class PaymentServiceImpl : IPaymentService {

        private readonly ApplicationDbContext _context;
        private readonly IOrderService _orderService;
        private readonly IConfiguration _config;
        private readonly ILogger<PaymentServiceImpl> _logger;

        public PaymentServiceImpl(
            ApplicationDbContext context,
            IOrderService orderService,
            IConfiguration config,
            ILogger<PaymentServiceImpl> logger) {
            _context = context;
            _orderService = orderService;
            _config = config;
            _logger = logger;
        }

        public async Task<string> CreatePreferenceAsync(Guid orderId) {
            var order = await _context.Orders
                .Include(o => o.Items)
                    .ThenInclude(i => i.ProductVariant)
                        .ThenInclude(v => v.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new NotFoundException($"La orden con ID {orderId} no existe.");

            var frontendUrl = _config["MercadoPago:frontendUrl"];
            var baseUrl = _config["MercadoPago:PublicBaseUrl"];

            var items = order.Items.Select(i => new PreferenceItemRequest {
                Title = i.ProductVariant?.Product?.Name ?? "Producto",
                Quantity = i.Quantity,
                CurrencyId = "ARS",
                UnitPrice = i.UnitPrice
            }).ToList();

            var request = new PreferenceRequest {
                Items = items,
                BackUrls = new PreferenceBackUrlsRequest {
                    Success = $"{frontendUrl}/payment/result/?status=approved",
                    Failure = $"{frontendUrl}/payment/result/?status=failure",
                    Pending = $"{frontendUrl}/payment/result/?status=pending"
                },
                AutoReturn = "approved",
                NotificationUrl = $"{baseUrl}/api/payment/webhook",
                ExternalReference = order.Id.ToString()
            };

            var client = new PreferenceClient();
            var preference = await client.CreateAsync(request);

            return preference.InitPoint;
        }

        public async Task ProcessWebhookAsync(string paymentId) {
            if (!long.TryParse(paymentId, out var parsedPaymentId)) {
                _logger.LogWarning("El PaymentId '{PaymentId}' no es un número válido.", paymentId);
                return;
            }

            Payment payment;
            try {
                var paymentClient = new PaymentClient();
                payment = await paymentClient.GetAsync(parsedPaymentId);
            } catch (MercadoPagoApiException ex) when (ex.StatusCode == (int)System.Net.HttpStatusCode.NotFound) {
                _logger.LogWarning("El pago con ID {PaymentId} no fue encontrado en Mercado Pago (404).", paymentId);
                return;
            } catch (Exception ex) {
                _logger.LogError(ex, "Error al consultar el pago con ID {PaymentId} en Mercado Pago.", paymentId);
                return;
            }

            _logger.LogWarning("Pago consultado: Status={Status} | ExternalReference={ExternalReference} | Amount={Amount}",
                payment.Status, payment.ExternalReference, payment.TransactionAmount);

            if (!Guid.TryParse(payment.ExternalReference, out var orderId)) {
                _logger.LogWarning("ExternalReference inválido o vacío, no se puede vincular a una Order.");
                return;
            }

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null) {
                _logger.LogWarning("No se encontró la Order {OrderId}", orderId);
                return;
            }

            if (order.Status != EOrderStatus.Pending) {
                _logger.LogWarning("La Order {OrderId} ya no está Pending (estado actual: {Status}), se ignora.", orderId, order.Status);
                return;
            }

            EOrderStatus? newStatus = payment.Status switch {
                "approved" => EOrderStatus.Paid,
                "rejected" => EOrderStatus.Cancelled,
                _ => null
            };

           

            if (newStatus.HasValue) {
                await _orderService.UpdateStatusAsync(orderId, newStatus.Value);
                _logger.LogWarning("Order {OrderId} actualizada a {NewStatus}", orderId, newStatus.Value);
            }
            if (newStatus == EOrderStatus.Paid) {
                await _orderService.SendOrderConfirmationEmailAsync(await _orderService.GetByIdAsync(orderId));
            }
        }

        public bool ValidateWebhookSignature(string paymentId, string xSignature, string xRequestId) {
            if (string.IsNullOrEmpty(xSignature) || string.IsNullOrEmpty(xRequestId) || string.IsNullOrEmpty(paymentId))
                return false;

            var parts = xSignature.Split(',');
            string? ts = null;
            string? v1 = null;

            foreach (var part in parts) {
                var keyValue = part.Split('=', 2);
                if (keyValue.Length != 2) continue;

                var key = keyValue[0].Trim();
                var value = keyValue[1].Trim();

                if (key == "ts") ts = value;
                if (key == "v1") v1 = value;
            }

            if (ts == null || v1 == null) return false;

            var manifest = $"id:{paymentId};request-id:{xRequestId};ts:{ts};";

            var secret = _config["MercadoPago:WebhookSecret"]?.Trim();

            if (string.IsNullOrEmpty(secret)) {
                _logger.LogError("MercadoPago:WebhookSecret está vacío o no configurado.");
                return false;
            }

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var calculatedHashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(manifest));

            byte[] v1Bytes;
            try {
                v1Bytes = Convert.FromHexString(v1);
            } catch (FormatException) {
                _logger.LogWarning("El header v1 no es un hexadecimal válido: {V1}", v1);
                return false;
            }

            _logger.LogInformation("DIAGNOSTICO - Secret usado (primeros 8): {SecretPrefix}...", secret.Length >= 8 ? secret[..8] : secret);
            _logger.LogInformation("DIAGNOSTICO - Manifest: {Manifest}", manifest);
            _logger.LogInformation("DIAGNOSTICO - Calculado: {Calculado} | Esperado: {Esperado}",
                Convert.ToHexString(calculatedHashBytes).ToLower(), v1.ToLower());

            return CryptographicOperations.FixedTimeEquals(calculatedHashBytes, v1Bytes);
        }
    }
}