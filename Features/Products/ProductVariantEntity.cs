namespace LaCasitaDeMiga.Features.Products {
    public class ProductVariantEntity {
        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Sku { get; set; } = null!;

        // 1. Precio de Venta (Público)
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; } // Opcional (null), para precios de oferta

        // 2. NUEVO: Precio de Compra (Última factura de proveedor)
        public decimal LastPurchasePrice { get; set; } = 0.00m;

        // 3. NUEVO: Precio Promedio (Costo Ponderado para Ganancias)
        public decimal AverageCost { get; set; } = 0.00m;

        public int Stock { get; set; }
        public int LowStockThreshold { get; set; } = 3;

        // La clave para el dinamismo total de cualquier rubro (JSONB)
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Propiedad de navegación (Relación inversa hacia el padre)
        public ProductEntity Product { get; set; } = null!;

      
    }
}