using System.ComponentModel.DataAnnotations;

namespace ECommersAPI.Features.Categories.DTOs {
    public class CategoryRequestDto {
        [Required(ErrorMessage = "El nombre de la categoría es obligatorio.")]
        [StringLength(100, ErrorMessage = "El nombre no puede superar los {1} caracteres.")]
        public string Name { get; set; } = string.Empty;

        // Si es una subcategoría, nos mandarán el ID del padre. Si es una categoría principal, vendrá NULL
        public Guid? ParentId { get; set; }
    }
}
