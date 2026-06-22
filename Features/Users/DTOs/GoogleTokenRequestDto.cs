using System.ComponentModel.DataAnnotations;

namespace LaCasitaDeMiga.Features.Users.DTOs {
    public class GoogleTokenRequestDto {
        [Required(ErrorMessage = "El ID Token de Google es obligatorio.")]
        public string IdToken { get; set; } = string.Empty;
    }
}