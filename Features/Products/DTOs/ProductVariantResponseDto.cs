namespace ECommersAPI.Features.Products.DTOs {
    public class ProductVariantResponseDto {
        public Guid Id { get; set; }
        public string Sku { get; set; } = null!;
        public decimal Price { get; set; }
        public decimal? CompareAtPrice { get; set; }
        public int Stock { get; set; }
        public int LowStockThreshold { get; set; }
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
        public bool IsActive { get; set; }

        // La propiedad calculada que inventaste para las alertas automáticas de stock bajo
        public bool IsLowStock => Stock <= LowStockThreshold;
    }
}
