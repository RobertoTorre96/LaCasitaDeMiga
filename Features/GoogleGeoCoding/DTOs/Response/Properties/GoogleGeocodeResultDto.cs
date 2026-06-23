using System.Text.Json.Serialization;

namespace LaCasitaDeMiga.Features.GoogleGeoCoding.DTOs.Response.Properties {
    public class GoogleGeocodeResultDto {
        [JsonPropertyName("geometry")]
        public GoogleGeometryDto Geometry { get; set; } = null!;
    }
}
