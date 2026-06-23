using LaCasitaDeMiga.Features.GoogleGeoCoding.DTOs.Response.Properties;
using System.Text.Json.Serialization;

namespace LaCasitaDeMiga.Features.GoogleGeoCoding.DTOs.Response {
    public class GoogleGeocodeResponseDto {
        [JsonPropertyName("status")]
        public string Status { get; set; } = null!;

        [JsonPropertyName("results")]
        public List<GoogleGeocodeResultDto> Results { get; set; }= new List<GoogleGeocodeResultDto>();

    }
}
