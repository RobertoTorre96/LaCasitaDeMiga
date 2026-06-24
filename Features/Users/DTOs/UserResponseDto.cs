using LaCasitaDeMiga.Features.Users.role;

namespace LaCasitaDeMiga.Features.Users.DTOs {
    public class UserResponseDto {

        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PictureUrl { get; set; } // Opcional, por si viene de Google
        public UserRole Role { get; set; } = UserRole.Customer;
        public bool IsActive { get; set; } // 💡 Agregado para que viaje al front
    }
}
