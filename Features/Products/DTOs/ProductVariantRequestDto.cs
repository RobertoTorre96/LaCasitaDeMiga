using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Products.DTOs {
    public class ProductVariantRequestDto {

        [StringLength(50, ErrorMessage = "El SKU no puede superar los 50 caracteres.")]
        public string? Sku { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Price { get; set; }
        // --- NUEVO CAMPO: COSTO DE COMPRA INICIAL ---
        [Required(ErrorMessage = "El precio de compra/costo es obligatorio para inicializar el stock.")]
        [Range(0.00, double.MaxValue, ErrorMessage = "El precio de compra no puede ser negativo.")]
        public decimal PurchasePrice { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio de oferta debe ser mayor a 0.")]
        public decimal? CompareAtPrice { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio.")]
        [Range(0, int.MaxValue, ErrorMessage = "El stock no puede ser negativo.")]
        public int Stock { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock crítico no puede ser negativo.")]
        public int LowStockThreshold { get; set; } = 3;
        
        [Range(0, int.MaxValue, ErrorMessage = "La prioridad no puede ser un número negativo.")]
        public int? Priority { get; set; } // Puede ser null al recibirlo

        public bool? IsFeatured { get; set; } // Puede ser null al recibirlo

        // Diccionario genérico para los atributos dinámicos (jsonb)
        // Ejemplo: { "Sabor": "Jamón y Queso", "Tamaño": "Copetín" }
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
    }
}