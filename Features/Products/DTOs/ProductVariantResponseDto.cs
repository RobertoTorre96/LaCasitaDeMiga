namespace LaCasitaDeMiga.Features.Products.DTOs {
    public class ProductVariantResponseDto {
        public Guid Id { get; set; }
        public string Sku { get; set; } = null!; // ◄ Se queda aquí para mostrarlo en las tablas/tarjetas
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }

        // --- LOS CAMPOS FINANCIEROS NUEVOS ---
        public decimal LastPurchasePrice { get; set; }
        public decimal AverageCost { get; set; }
        // ─────────────────────────────────────

        // --- NUEVOS CAMPOS DE CONTROL Y CONCURRENCIA ---
        public int Priority { get; set; }
        public bool IsFeatured { get; set; }
        public int Version { get; set; } // Opcional dejarlo, sirve de info para el Front

        public int Stock { get; set; }
        public int LowStockThreshold { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        public bool IsActive { get; set; }

        // La propiedad calculada para las alertas automáticas de stock bajo
        public bool IsLowStock => Stock <= LowStockThreshold;
    }
}