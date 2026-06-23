namespace LaCasitaDeMiga.Features.GoogleGeoCoding.Services {
    public interface IGoogleGeocodingService {

        /// <summary>
        /// Convierte una dirección de texto en coordenadas geográficas (Latitud y Longitud).
        /// </summary>
        /// <param name="address">La dirección ingresada por el usuario (ej: "Las delicias 3235, San Miguel").</param>
        /// <returns>Una tupla con Latitud y Longitud, o null si no se encuentra la dirección.</returns>
        Task<(double Lat, double Lon)?> GetCoordinatesAsync(string address);

    }
}
