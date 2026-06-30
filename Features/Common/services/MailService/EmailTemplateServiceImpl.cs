using LaCasitaDeMiga.Features.Common.services.MailService.Enums;
using LaCasitaDeMiga.Features.Orders.DTOs;
using System.Text;
using System.Text.Json;

namespace LaCasitaDeMiga.Features.Common.services.MailService {
    public class EmailTemplateServiceImpl : IEmailTemplateService {

        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public EmailTemplateServiceImpl(IConfiguration configuration) {
            _configuration = configuration;
            _httpClient = new HttpClient(); // Mantenemos tu enfoque con HttpClient
        }

        public Task SendOrderConfirmationEmailAsync(ComboEspecialDTO order) {
            throw new NotImplementedException();
        }

        public async Task SendTemplateEmailAsync(string toEmail, EEmailTemplate templateId, object parameters) {
            var apiKey = _configuration["Brevo:ApiKey"] ?? _configuration["Brevo__ApiKey"];

            if (string.IsNullOrEmpty(apiKey)) {
                throw new Exception("La API Key de Brevo no está configurada correctamente en el nuevo servicio.");
            }

            // Estructura exacta que pide Brevo para transacciones con plantilla
            var emailData = new {
                to = new[] { new { email = toEmail } },
                templateId = templateId,
                @params = parameters // El objeto anónimo con USER_NAME y RESET_LINK
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode) {
                var errorResponse = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error en la API de Brevo (Servicio Plantillas): {errorResponse}");
            }
        }
    }
}
