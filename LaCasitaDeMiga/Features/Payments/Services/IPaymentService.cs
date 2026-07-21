namespace LaCasitaDeMiga.Features.Payments.Services {
    public interface IPaymentService {
        Task<string> CreatePreferenceAsync(Guid orderId);
        Task ProcessWebhookAsync(string paymentId);
        bool ValidateWebhookSignature(string paymentId, string xSignature, string xRequestId); 

    }
}