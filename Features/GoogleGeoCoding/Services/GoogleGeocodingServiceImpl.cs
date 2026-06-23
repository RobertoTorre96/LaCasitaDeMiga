using LaCasitaDeMiga.Features.GoogleGeoCoding.DTOs.Response;
using System.Text.Json.Serialization;

namespace LaCasitaDeMiga.Features.GoogleGeoCoding.Services {
    public class GoogleGeocodingServiceImpl : IGoogleGeocodingService{

        private readonly HttpClient _httpClient;
        private readonly string _apiKey ;

        public GoogleGeocodingServiceImpl(HttpClient httpClient, IConfiguration configuration) {
            _httpClient = httpClient;
            // Esto lee la clave que guardamos en tu appsettings.json
            _apiKey = configuration["GoogleMaps:ApiKey"]?? string.Empty;
        }

        public async Task<(double Lat, double Lon)?> GetCoordinatesAsync(string address) {
            // Le sumamos Buenos Aires, Argentina para acotar la búsqueda automáticamente
            string fullAddress = $"{address}, Buenos Aires, Argentina";

            // Armamos la URL oficial de Google Maps
            var url = $"https://maps.googleapis.com/maps/api/geocode/json" +
                      $"?address={Uri.EscapeDataString(fullAddress)}" +
                      $"&components=country:AR" + // Forzamos a que solo busque dentro de Argentina
                      $"&key={_apiKey}";

            try {
                var response = await _httpClient.GetFromJsonAsync<GoogleGeocodeResponseDto>(url);

                // Si Google encuentra la dirección, nos devuelve "OK"
                if (response != null && response.Status == "OK" && response.Results.Count > 0) {
                    var location = response.Results[0].Geometry.Location;
                    return (location.Lat, location.Lng);
                }
            } catch (Exception) {
                // Si hay un error de red o de conexión, acá podrías meter un log
                throw new Exception ("Error al conectarse con el servicio de Google Maps.");
            }

            return null; // Si no encontró nada o falló, devuelve null
        }
    }    
}
