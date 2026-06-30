using LaCasitaDeMiga.Features.Users.role;

namespace LaCasitaDeMiga.Features.Users {
    public class UserEntity {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? PictureUrl { get; set; }
        public string? PasswordHash { get; set; }
        public bool IsActive { get; set; } = true; 
        public UserRole Role { get; set; } = UserRole.Customer;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}
