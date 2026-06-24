using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Users.DTOs {
    public class ForgotPasswordDto {
        [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
        [EmailAddress(ErrorMessage = "El formato del correo electrónico no es válido.")]
        public string Email { get; set; } = string.Empty;
    }
}
