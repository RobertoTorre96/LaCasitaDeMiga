using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace ECommersAPI.Features.Products {
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

            // Mapeo correcto para dinero (numeric en BD -> decimal en C#)
            builder.Property(v => v.Price)
                .HasColumnName("price")
                .HasColumnType("numeric(12,2)")
                .IsRequired();

            builder.Property(v => v.CompareAtPrice)
                .HasColumnName("compare_at_price")
                .HasColumnType("numeric(12,2)");

            builder.Property(v => v.Stock)
                .HasColumnName("stock")
                .HasDefaultValue(0);

            // Configuramos tu nuevo campo de Stock Crítico
            builder.Property(v => v.LowStockThreshold)
                .HasColumnName("low_stock_threshold")
                .HasDefaultValue(3);

            builder.Property(v => v.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);

            builder.Property(v => v.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 3. SERIALIZACIÓN JSONB: El puente entre C# y PostgreSQL
            builder.Property(v => v.Attributes)
                .HasColumnName("attributes")
                .HasColumnType("jsonb")
                // HasConversion transforma los datos al ir y venir de la BD
                .HasConversion(
                    // Al GUARDAR: Convierte el Diccionario C# a un String JSON
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
                    // Al LEER: Convierte el String JSON de la BD de vuelta a un Diccionario C#
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions)null!)
                         ?? new Dictionary<string, object>()
                )
                .HasDefaultValueSql("'{}'::jsonb");

            // 4. Configuración de Índices (Tal cual los creaste en DBeaver)
            builder.HasIndex(v => v.Sku)
                .IsUnique()
                .HasDatabaseName("product_variants_sku_key");

            builder.HasIndex(v => v.ProductId)
                .HasDatabaseName("idx_variants_product");

            // 5. Relación con el Padre (Producto) e Integridad Referencial
            builder.HasOne(v => v.Product)
                .WithMany(p => p.Variants)   // Un producto tiene muchas variantes
                .HasForeignKey(v => v.ProductId)
                .OnDelete(DeleteBehavior.Cascade); // ON DELETE CASCADE (Tu regla del DDL)
        }
    }

}
    

