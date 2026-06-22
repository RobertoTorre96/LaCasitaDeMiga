namespace LaCasitaDeMiga.Features.Users.DTOs {
    public class UserResponseDto {

        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PictureUrl { get; set; } // Opcional, por si viene de Google
        public string Role { get; set; } = "Customer";
    }
}
