using System.Text.Json.Serialization;

namespace LaCasitaDeMiga.Features.GoogleGeoCoding.DTOs.Response.Properties {
    public class GoogleGeometryDto {
        [JsonPropertyName("location")]
        public GoogleLocationDto Location { get; set; } = null!;
    }
}
