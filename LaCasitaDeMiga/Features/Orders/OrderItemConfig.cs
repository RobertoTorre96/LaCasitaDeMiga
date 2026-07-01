using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaCasitaDeMiga.Features.Orders {
    public class OrderItemConfig : IEntityTypeConfiguration <OrderItemEntity> {

        public void Configure(EntityTypeBuilder<OrderItemEntity> builder) {
            builder.ToTable("OrderItems");
            builder.HasKey(i => i.Id);

            builder.Property(i => i.Quantity).IsRequired();

            builder.Property(i => i.UnitPrice)
                .HasColumnType("decimal(18,2)")
                .IsRequired();
            // --- NUEVO MAPEO PARA EL COSTO HISTÓRICO ---
            builder.Property(i => i.UnitCost)
                .HasColumnType("decimal(18,2)")
                .HasDefaultValue(0.00m)
                .IsRequired();

            builder.HasOne(i => i.ProductVariant)
                .WithMany()
                .HasForeignKey(i => i.ProductVariantId)
                .OnDelete(DeleteBehavior.Restrict); // Protegemos el histórico
        }
    }
}
