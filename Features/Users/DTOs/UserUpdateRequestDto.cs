using LaCasitaDeMiga.Features.Users.role;
using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Users.DTOs {
    public class UserUpdateRequestDto {
        [Required(ErrorMessage = "El estado de actividad (IsActive) es obligatorio.")]
        public bool IsActive { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio.")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "El rol ingresado no es un rol válido para el sistema.")]
        public UserRole Role { get; set; }
    }
}
