using System.Text.Json.Serialization;

namespace LaCasitaDeMiga.Features.GoogleGeoCoding.DTOs.Response.Properties {
    public class GoogleLocationDto {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lng")]
        public double Lng { get; set; }
    }
}
