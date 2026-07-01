using LaCasitaDeMiga.Features.Common.services.MailService.Enums;
using LaCasitaDeMiga.Features.Orders.DTOs;

namespace LaCasitaDeMiga.Features.Common.services.MailService {
    public interface IEmailTemplateService {
        Task SendTemplateEmailAsync(string toEmail, EEmailTemplate templateId, object parameters);
        Task SendOrderConfirmationEmailAsync(ComboEspecialDTO order);
    }
}
