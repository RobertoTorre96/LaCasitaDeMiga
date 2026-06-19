namespace ECommersAPI.Features.Products {
    public class ProductVariantEntity {

        public Guid Id { get; set; }
        public Guid ProductId { get; set; }
        public string Sku { get; set; } = null!;

        // Usamos decimal en C# para mapear el numeric(12,2) de la BD (ideal para dinero)
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; } // Opcional (null), para precios de oferta

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
