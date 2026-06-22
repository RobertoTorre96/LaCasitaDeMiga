namespace LaCasitaDeMiga.Features.Users {
    public class UserEntity {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? PictureUrl { get; set; }
        public string? PasswordHash { get; set; }
        public string Role { get; set; } = "Customer";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
