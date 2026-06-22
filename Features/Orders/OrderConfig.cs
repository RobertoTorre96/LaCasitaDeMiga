using LaCasitaDeMiga.Features.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaCasitaDeMiga.Features.Orders {
    public class OrderConfig : IEntityTypeConfiguration<OrderEntity> {
        public void Configure(EntityTypeBuilder<OrderEntity> builder) {
            builder.ToTable("Orders");
            builder.HasKey(o => o.Id);

            builder.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Guardamos el Enum como un String legible en PostgreSQL
            builder.Property(o => o.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(o => o.CreatedAt).IsRequired();

            // --- NUEVA RELACIÓN: Vinculamos la orden con el usuario (Customer) ---
            builder.HasOne(o => o.Customer)
                .WithMany() // Un usuario puede tener muchas órdenes
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict); // Evita borrar usuarios con compras hechas
            // -------------------------------------------------------------------

            // Relación con los ítems (Uno a Muchos)
            builder.HasMany(o => o.Items)
                .WithOne(i => i.Order)
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
} 