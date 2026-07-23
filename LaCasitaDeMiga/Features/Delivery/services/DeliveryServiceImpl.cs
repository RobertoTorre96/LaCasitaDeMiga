using LaCasitaDeMiga.Features.Delivery.Enums;
using LaCasitaDeMiga.Features.GoogleGeoCoding.Services;

namespace LaCasitaDeMiga.Features.Delivery.services {
    public class DeliveryServiceImpl :IDeliveryService{
        private readonly IGoogleGeocodingService _geocodingService;

        // COORDENADAS DE "LA CASITA DE MIGA"
        private const double ShopLat = -34.54606028294171;
        private const double ShopLon = -58.744249257393115;

        // LÍMITES DE LAS ZONAS EN KILÓMETROS
        private const double Zone1MaxKm = 3.0;
        private const double Zone2MaxKm = 10.0;
        private const double Zone3MaxKm = 15.0;

        public DeliveryServiceImpl(IGoogleGeocodingService geocodingService) {
            _geocodingService = geocodingService;
        }

        public async Task<EDeliveryZone> GetDeliveryZoneAsync(string address) {
            // 1. Convertimos la dirección a coordenadas con Google Geocoding
            var customerCoordinates = await _geocodingService.GetCoordinatesAsync(address);

            if (customerCoordinates == null) {
                // Si la dirección no existe o no se pudo geolocalizar
                return EDeliveryZone.OutOfZone;
            }

            // 2. Calculamos la distancia en KM
            double distance = CalculateHaversineDistance(
                ShopLat, ShopLon,
                customerCoordinates.Value.Lat, customerCoordinates.Value.Lon
            );

            // 3. Evaluamos de menor a mayor rango
            if (distance <= Zone1MaxKm) {
                return EDeliveryZone.Zone1;
            }

            if (distance <= Zone2MaxKm) {
                return EDeliveryZone.Zone2;
            }

            if (distance <= Zone3MaxKm) {
                return EDeliveryZone.Zone3;
            }

            // Si supera los 15 km
            return EDeliveryZone.OutOfZone;
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
