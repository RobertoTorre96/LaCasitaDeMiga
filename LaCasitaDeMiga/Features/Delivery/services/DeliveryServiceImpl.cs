using LaCasitaDeMiga.Features.GoogleGeoCoding.Services;

namespace LaCasitaDeMiga.Features.Delivery.services {
    public class DeliveryServiceImpl :IDeliveryService{
        private readonly IGoogleGeocodingService _geocodingService;
        // COORDENADAS DE "LA CASITA DE MIGA" (Modifícalas por las reales de tu local) -34.54606028294171, -58.744249257393115
        private const double ShopLat = -34.54606028294171;
        private const double ShopLon = -58.744249257393115;

        // Radio de cobertura máximo
        private const double MaxDistanceKm = 15.0;

        public DeliveryServiceImpl(IGoogleGeocodingService geocodingService) {
            _geocodingService = geocodingService;
        }

        public async Task<bool> IsAddressInDeliveryZoneAsync(string address) {
            // 1. Convertimos la dirección de texto a coordenadas con el servicio de Google
            var customerCoordinates = await _geocodingService.GetCoordinatesAsync(address);

            if (customerCoordinates == null) {
                // Si Google no encontró la dirección, asumimos que no podemos enviar
                return false;
            }

            // 2. Calculamos la distancia en kilómetros entre el local y el cliente
            double distance = CalculateHaversineDistance(
                ShopLat, ShopLon,
                customerCoordinates.Value.Lat, customerCoordinates.Value.Lon
            );

            // 3. Si la distancia es menor o igual a 15 km, devolvemos true
            return distance <= MaxDistanceKm;
        }

        // Algoritmo matemático Haversine
        private double CalculateHaversineDistance(double lat1, double lon1, double lat2, double lon2) {
            const double EarthRadiusKm = 6371.0;

            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            return EarthRadiusKm * c;
        }

        private double ToRadians(double angle) => (Math.PI / 180) * angle;

    }
}
