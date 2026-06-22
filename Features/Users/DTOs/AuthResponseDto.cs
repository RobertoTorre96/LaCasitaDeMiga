namespace LaCasitaDeMiga.Features.Users.DTOs {
    public class AuthResponseDto {
        // Contiene la información pública del usuario (sin contraseñas)
        public UserResponseDto User { get; set; } = null!;

        // Contiene el token JWT larguísimo que generamos en el servicio
        public string Token { get; set; } = string.Empty;
    }
}