using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECommersAPI.Features.Orders {
    public class OrderConfig : IEntityTypeConfiguration<OrderEntity> {
        public void Configure(EntityTypeBuilder<OrderEntity> builder) {
            builder.ToTable("Orders");
            builder.HasKey(o => o.Id);

            builder.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // CORRECCIÓN: Guardamos el Enum como un String legible en PostgreSQL
            builder.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(o => o.CreatedAt).IsRequired();

            builder.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
