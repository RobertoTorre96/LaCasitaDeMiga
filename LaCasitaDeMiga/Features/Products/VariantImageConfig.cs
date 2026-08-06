using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaCasitaDeMiga.Features.Products {
    public class VariantImageConfig : IEntityTypeConfiguration<VariantImageEntity> {

        public void Configure(EntityTypeBuilder<VariantImageEntity> builder) {
            builder.ToTable("variant_images");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.ImageUrl)
                .HasColumnName("image_url")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(i => i.PublicId)
                .HasColumnName("public_id")
                .HasMaxLength(255)
                .IsRequired();

            // Relación con ProductVariant (Cascade: si se borra la variante, se borran sus registros de imágenes)
            builder.HasOne(i => i.ProductVariant)
                .WithMany(v => v.Images)
                .HasForeignKey(i => i.ProductVariantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
