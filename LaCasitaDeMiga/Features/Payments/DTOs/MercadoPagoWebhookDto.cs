using System.Text.Json.Serialization;

namespace LaCasitaDeMiga.Features.Payments.DTOs {
    public class MercadoPagoWebhookDto {

        // Formato nuevo: "type": "payment"
        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("data")]
        public MercadoPagoWebhookDataDto? Data { get; set; }

        // Formato viejo (legacy/IPN): "topic": "payment", "resource": "169317845044"
        [JsonPropertyName("topic")]
        public string? Topic { get; set; }

        [JsonPropertyName("resource")]
        public string? Resource { get; set; }

        // Propiedad calculada: unifica ambos formatos en un solo valor útil
        [JsonIgnore]
        public string? EventKind => Type ?? Topic;

        [JsonIgnore]
        public string? PaymentId => Data?.Id ?? Resource;
    }

    public class MercadoPagoWebhookDataDto {
        [JsonPropertyName("id")]
        public string? Id { get; set; } 
    }
}