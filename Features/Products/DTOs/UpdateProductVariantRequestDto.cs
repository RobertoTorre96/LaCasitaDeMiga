using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Products.DTOs {
    public class UpdateProductVariantRequestDto {

        [StringLength(50, ErrorMessage = "El SKU no puede superar los 50 caracteres.")]
        public string? Sku { get; set; }

        [Required(ErrorMessage = "El precio es obligatorio.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0.")]
        public decimal Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "El precio de oferta debe ser mayor a 0.")]
        public decimal? CompareAtPrice { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "El stock crítico no puede ser negativo.")]
        public int LowStockThreshold { get; set; } = 3;

        // --- NUEVOS CAMPOS OPCIONALES ---
        [Range(0, int.MaxValue, ErrorMessage = "La prioridad no puede ser un número negativo.")]
        public int? Priority { get; set; }

        public bool? IsFeatured { get; set; }


        // Permitimos que editen sus atributos específicos (ej: cambiar el Sabor si se cargó mal)
        public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();

        public bool IsActive { get; set; } = true;
    }
}
