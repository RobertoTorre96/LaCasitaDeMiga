using System.ComponentModel.DataAnnotations;

namespace ECommersAPI.Features.Brands.DTOs {
    public class BrandRequestDto {

        [Required(ErrorMessage = "El nombre de la marca es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre de la marca no puede superar los 100 caracteres.")]
        public string Name { get; set; } = null!;

        [Url(ErrorMessage = "El logo debe ser una URL válida.")]
        [StringLength(255, ErrorMessage = "La URL del logo no puede superar los 255 caracteres.")]
        public string? LogoUrl { get; set; }
    }
}

