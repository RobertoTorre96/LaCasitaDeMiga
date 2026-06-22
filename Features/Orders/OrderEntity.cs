using LaCasitaDeMiga.Features.Users; // Asegúrate de importar el namespace donde está tu UserEntity

namespace LaCasitaDeMiga.Features.Orders {
    public class OrderEntity {
        public Guid Id { get; set; } = Guid.NewGuid();

        // 1. Esta sigue siendo la clave foránea en la base de datos
        public Guid CustomerId { get; set; }

        // 2. PROPIEDAD DE NAVEGACIÓN: La relación física al objeto Usuario
        public UserEntity Customer { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Estado de la orden (Usa nuestro Enum)
        public EOrderStatus Status { get; set; } = EOrderStatus.Pending;

        // Monto total acumulado de la orden
        public decimal TotalAmount { get; set; }

        // Relación "Uno a Muchos": Una orden tiene muchos ítems
        public ICollection<OrderItemEntity> Items { get; set; } = new List<OrderItemEntity>();
    }
}