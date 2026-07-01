using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Users.DTOs {
    public class ResetPasswordDto {
        [Required(ErrorMessage = "El token de recuperación es obligatorio.")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es obligatoria.")]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
