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

        public EmailServiceImpl(IConfiguration configuration) {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body) {
            var senderEmail = _configuration["Gmail:Email"] ?? _configuration["Gmail__Email"];
            var appPassword = _configuration["Gmail:AppPassword"] ?? _configuration["Gmail__AppPassword"];

            if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(appPassword)) {
                throw new Exception("Las credenciales de Gmail no están configuradas.");
            }

            // Usamos el cliente nativo de .NET apuntando al servidor de Google
            using (var client = new SmtpClient("smtp.gmail.com", 587)) {
                client.EnableSsl = true; // Activa TLS de forma segura
                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(senderEmail, appPassword);

                var mailMessage = new MailMessage {
                    From = new MailAddress(senderEmail, "La Casita de Miga"),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                try {
                    // Enviamos de forma asincrónica usando las credenciales seguras (App Password)
                    await client.SendMailAsync(mailMessage);
                } catch (Exception ex) {
                    throw new Exception($"Error al despachar el correo mediante Gmail: {ex.Message}");
                }
            }
        }

    }
}
