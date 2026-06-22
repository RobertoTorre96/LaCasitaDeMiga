using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaCasitaDeMiga.Features.Products {
    public class ProductConfig : IEntityTypeConfiguration<ProductEntity> {
        public void Configure(EntityTypeBuilder<ProductEntity> builder) {
            builder.ToTable("products");
            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id).HasColumnName("id");
            builder.Property(p => p.Name)
                            .HasColumnName("name")
                            .HasMaxLength(150)
                            .IsRequired();

            builder.Property(p => p.Slug)
                .HasColumnName("slug")
                .HasMaxLength(180)
                .IsRequired();

            builder.Property(p => p.Description)
                .HasColumnName("description")
                .HasColumnType("text")
                .IsRequired();

            builder.Property(p => p.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            builder.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            builder.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 3. Mapeo de Columnas de Claves Foráneas (FK)
            builder.Property(p => p.CategoryId).HasColumnName("category_id");
            builder.Property(p => p.BrandId).HasColumnName("brand_id");

            // 4. Configuración de Índices (Tal como los tienes en el DDL)
            builder.HasIndex(p => p.Slug)
                .IsUnique()
                .HasDatabaseName("idx_products_slug");

            builder.HasIndex(p => p.CategoryId)
                .HasDatabaseName("idx_products_category");

            // 5. Configuración de Relaciones e Integridad Referencial
            // Relación con Categoría (Requerida - ON DELETE RESTRICT)
            builder.HasOne(p => p.Category)
                .WithMany() // Si tu CategoryEntity no tiene una lista de productos, se deja vacío
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Relación con Marca (Opcional - ON DELETE SET NULL)
            builder.HasOne(p => p.Brand)
                .WithMany() // Si tu BrandEntity no tiene una lista de productos, se deja vacío
                .HasForeignKey(p => p.BrandId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
