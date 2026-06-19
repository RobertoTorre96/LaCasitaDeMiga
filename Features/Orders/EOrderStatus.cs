    namespace ECommersAPI.Features.Orders {
    public enum EOrderStatus {
        Pending = 1,   // El cliente inició el checkout pero aún no pagó
        Paid = 2,      // Pago confirmado, listo para preparar
        Shipped = 3,   // En camino al domicilio del cliente
        Completed = 4, // Entregado con éxito
        Cancelled = 5  // Cancelado por falta de stock, pago rechazado, etc.
    }
}
