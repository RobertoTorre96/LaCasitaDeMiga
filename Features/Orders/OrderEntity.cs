namespace ECommersAPI.Features.Orders {
    public class OrderEntity {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Por ahora lo dejamos como Guid plano. Cuando tengamos el módulo 
        // de Usuarios, acá habrá una relación física a la tabla de Users.
        public Guid CustomerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Estado de la orden (Usa nuestro Enum)
        public EOrderStatus Status { get; set; } = EOrderStatus.Pending;

        // Monto total acumulado de la orden
        public decimal TotalAmount { get; set; }

        // Relación "Uno a Muchos": Una orden tiene muchos ítems
        public ICollection<OrderItemEntity> Items { get; set; } = new List<OrderItemEntity>();
    }
}
