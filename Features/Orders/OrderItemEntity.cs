using LaCasitaDeMiga.Features.Products;

namespace LaCasitaDeMiga.Features.Orders {
    public class OrderItemEntity {
        public Guid Id { get; set; } = Guid.NewGuid();

        // Relación con el Padre (La Orden)
        public Guid OrderId { get; set; }
        public OrderEntity Order { get; set; } = null!;

        // Relación con el Producto comprado (La Variante)
        public Guid ProductVariantId { get; set; }
        public ProductVariantEntity ProductVariant { get; set; } = null!;

        // Datos históricos de la venta (Copias de seguridad)
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal UnitCost { get; set; }
    }
}
