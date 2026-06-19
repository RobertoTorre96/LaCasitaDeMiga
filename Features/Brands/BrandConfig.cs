using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommersAPI.Features.Brands {
    public class BrandConfig : IEntityTypeConfiguration<BrandEntity> {
        public void Configure(EntityTypeBuilder<BrandEntity> builder) {
            
            builder.ToTable("brands");
            builder.HasKey(b => b.Id);
            builder.Property(b => b.Id).HasColumnName("id");

            builder.Property(b => b.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
            builder.Property(b => b.LogoUrl).HasColumnName("logo_url").HasMaxLength(255);
            builder.Property(b => b.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("CURRENT_TIMESTAMP");

        }
    }
}
