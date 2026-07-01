namespace LaCasitaDeMiga.Features.Delivery.services {
    public interface IDeliveryService {

        /// <summary>
        /// Valida si una dirección está dentro del rango de cobertura (15 Km).
        /// </summary>
        Task<bool> IsAddressInDeliveryZoneAsync(string address);

    }
}
