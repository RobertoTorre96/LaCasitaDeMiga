using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text;
using System.Text.Json;

namespace LaCasitaDeMiga.Features.Common.services.MailService {
    public class EmailServiceImpl :IEmailService {

        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public EmailServiceImpl(IConfiguration configuration) {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body) {
            // Busca la clave tanto en local (Brevo:ApiKey) como en Railway (Brevo__ApiKey)
            var apiKey = _configuration["Brevo:ApiKey"] ?? _configuration["Brevo__ApiKey"];

            if (string.IsNullOrEmpty(apiKey)) {
                throw new Exception("La API Key de Brevo no está configurada correctamente.");
            }

            // Estructura JSON que exige la API de Brevo (usamos tu mail de registro como remitente)
            var emailData = new {
                sender = new { name = "La Casita de Miga", email = "metalerolml555@gmail.com" },
                to = new[] { new { email = toEmail } },
                subject = subject,
                htmlContent = body
            };

            var json = JsonSerializer.Serialize(emailData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Preparamos la petición HTTP POST hacia los servidores de Brevo (Puerto 443 web, libre)
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            // Si falla, capturamos el porqué para verlo en la consola
            if (!response.IsSuccessStatusCode) {
                var errorResponse = await response.Content.ReadAsStringAsync();
                throw new Exception($"Error en la API de Brevo: {errorResponse}");
            }
        }

    }
}
