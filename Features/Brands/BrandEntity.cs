namespace ECommersAPI.Features.Brands {
    public class BrandEntity {

        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string LogoUrl { get; set; } = null!;
        public DateTime CreatedAt { get; set; }= DateTime.UtcNow;

    }
}
