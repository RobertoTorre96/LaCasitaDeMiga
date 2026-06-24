using MimeKit;
using MimeKit.Text;          // 💡 Soluciona: TextFormat.Html
using MailKit.Security;      // 💡 Soluciona: SecureSocketOptions
using MailKit.Net.Smtp;

namespace LaCasitaDeMiga.Features.Common.services.MailService {
    public class EmailServiceImpl :IEmailService {

        private readonly IConfiguration _configuration;

        public EmailServiceImpl(IConfiguration configuration) {
            _configuration = configuration;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string body) {
            // 1. Creamos la estructura del mensaje
            var email = new MimeMessage();

            // Remitente (Vos)
            email.From.Add(new MailboxAddress(
                _configuration["EmailSettings:SenderName"],
                _configuration["EmailSettings:SenderEmail"]
            ));

            // Destinatario (El cliente)
            email.To.Add(MailboxAddress.Parse(toEmail));

            // Asunto y Cuerpo (Soporta HTML para que quede lindo)
            email.Subject = subject;
            email.Body = new TextPart(TextFormat.Html) { Text = body };

            // 2. Nos conectamos al servidor SMTP de Google y enviamos
            using var smtp = new SmtpClient();
            try {
                // Conexión segura por puerto 587 (StartTls)
                await smtp.ConnectAsync(
                    _configuration["EmailSettings:SmtpServer"],
                    int.Parse(_configuration["EmailSettings:Port"]!),
                    SecureSocketOptions.StartTls
                );

                // Autenticación con tu usuario y la contraseña de 16 letras
                await smtp.AuthenticateAsync(
                    _configuration["EmailSettings:Username"],
                    _configuration["EmailSettings:Password"]
                );

                // Despacho del mail
                await smtp.SendAsync(email);
            } finally {
                // Pase lo que pase, nos desconectamos limpiamente del servidor
                await smtp.DisconnectAsync(true);
            }
        }

    }
}
