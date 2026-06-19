using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommersAPI.Features.Categories {
    public class CategoryConfig : IEntityTypeConfiguration<CategoryEntity> {
        public void Configure(EntityTypeBuilder<CategoryEntity> builder) {
            builder.ToTable("categories");

            builder.HasKey(c => c.Id);
            builder.Property(c => c.Id).HasColumnName("id");

            
            builder.Property(c => c.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            builder.Property(c => c.Slug).HasColumnName("slug").HasMaxLength(120).IsRequired();
            builder.Property(c => c.ParentId).HasColumnName("parent_id");
            builder.Property(c => c.CreatedAt).HasColumnName("created_at");

            // Índice UNIQUE
            builder.HasIndex(c => c.Slug).IsUnique();

            // Relación jerárquica adaptada a CategoryEntity
            builder.HasOne(c => c.ParentCategory)
                   .WithMany(c => c.SubCategories)
                   .HasForeignKey(c => c.ParentId)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
