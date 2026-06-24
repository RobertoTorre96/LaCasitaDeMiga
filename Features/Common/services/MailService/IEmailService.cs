namespace LaCasitaDeMiga.Features.Common.services.MailService {
    public interface IEmailService {
        Task SendEmailAsync(string toEmail, string subject, string body);
    }
}
