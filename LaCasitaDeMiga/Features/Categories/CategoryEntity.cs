namespace LaCasitaDeMiga.Features.Categories {
    public class CategoryEntity    {

        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;

        // Relación Autoreferencial
        public Guid? ParentId { get; set; }
        public virtual CategoryEntity? ParentCategory { get; set; }
        public virtual ICollection<CategoryEntity> SubCategories { get; set; } = new List<CategoryEntity>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
