using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Products.DTOs {
    public class ProductVariantRequestDto {
        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio de oferta debe ser mayor a 0.")]
        public decimal? CompareAtPrice { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock crítico no puede ser negativo.")]
        public int LowStockThreshold { get; set; } = 3;

        // Diccionario genérico para los atributos dinámicos (jsonb)
        // Ejemplo: { "Sabor": "Jamón y Queso", "Tamaño": "Copetín" }
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }
}