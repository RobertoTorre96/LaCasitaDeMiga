using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace LaCasitaDeMiga.Features.Products {
    public class ProductVariantConfig : IEntityTypeConfiguration<ProductVariantEntity> {
        public void Configure(EntityTypeBuilder<ProductVariantEntity> builder) {

            // 1. Mapeo de Tabla y Llave Primaria
            builder.ToTable("product_variants");
            builder.HasKey(v => v.Id);
            builder.Property(v => v.Id).HasColumnName("id");

            // 2. Mapeo de Propiedades Básicas y Claves Foráneas
            builder.Property(v => v.ProductId).HasColumnName("product_id");

            builder.Property(v => v.Sku)
                .HasColumnName("sku")
                .HasMaxLength(50)
                .IsRequired();

            // 3. SERIALIZACIÓN JSONB
            builder.Property(v => v.Attributes)
                .HasColumnName("attributes")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!)
                         ?? new Dictionary<string, object>()
                )
                .HasDefaultValueSql("'{}'::jsonb")
                // ◄ ¡AGREGÁ ESTO ACÁ ABAJO para sacar la advertencia!
                .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<Dictionary<string, object>>(
                    (c1, c2) => JsonSerializer.Serialize(c1, (JsonSerializerOptions)null!) == JsonSerializer.Serialize(c2, (JsonSerializerOptions)null!),
                    c => c == null ? 0 : JsonSerializer.Serialize(c, (JsonSerializerOptions)null!).GetHashCode(),
                    c => JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(c, (JsonSerializerOptions)null!), (JsonSerializerOptions)null!)!
                ));

            // Mapeo correcto para dinero (numeric en BD -> decimal en C#)
            builder.Property(v => v.Price)
                .HasColumnName("price")
                .HasColumnType("numeric(12,2)")
                .IsRequired();

            builder.Property(v => v.CompareAtPrice)
                .HasColumnName("compare_at_price")
                .HasColumnType("numeric(12,2)");

            // --- MAPEOS PARA COSTOS ---
            builder.Property(v => v.LastPurchasePrice)
                .HasColumnName("last_purchase_price")
                .HasColumnType("numeric(12,2)")
                .HasDefaultValue(0.00m)
                .IsRequired();

            builder.Property(v => v.AverageCost)
                .HasColumnName("average_cost")
                .HasColumnType("numeric(12,2)")
                .HasDefaultValue(0.00m)
                .IsRequired();

            // --- NUEVOS MAPEOS: PRIORIDAD Y DESTACADO ---
            builder.Property(v => v.Priority)
                .HasColumnName("priority")
                .HasDefaultValue(0)
                .IsRequired();

            builder.Property(v => v.IsFeatured)
                .HasColumnName("is_featured")
                .HasDefaultValue(false)
                .IsRequired();
            // ───────────────────────────────────────────
            builder.Property(v => v.Version)
                .HasColumnName("version")
                .HasDefaultValue(1)
                .IsConcurrencyToken(); // <--- CRÍTICO: Activa la protección de EF Core

            builder.Property(v => v.Stock)
                .HasColumnName("stock")
                .HasDefaultValue(0);

            builder.Property(v => v.LowStockThreshold)
                .HasColumnName("low_stock_threshold")
                .HasDefaultValue(3);

            builder.Property(v => v.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            builder.Property(v => v.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 3. SERIALIZACIÓN JSONB
            builder.Property(v => v.Attributes)
                .HasColumnName("attributes")
                .HasColumnType("jsonb")
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!)
                         ?? new Dictionary<string, object>()
                )
                .HasDefaultValueSql("'{}'::jsonb");

            // 4. Configuración de Índices
            builder.HasIndex(v => v.Sku)
                .IsUnique()
                .HasDatabaseName("product_variants_sku_key");

            builder.HasIndex(v => v.ProductId)
                .HasDatabaseName("idx_variants_product");

            // 5. Relación con el Padre
            builder.HasOne(v => v.Product)
                .WithMany(p => p.Variants)
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}