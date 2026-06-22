using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LaCasitaDeMiga.Features.Users {
    public class UserConfig : IEntityTypeConfiguration<UserEntity> {
        public void Configure(EntityTypeBuilder<UserEntity> builder) {

            builder.ToTable("users");

            builder.HasKey(u => u.Id);
            builder.Property(u => u.Id).HasColumnName("id");

            builder.Property(u => u.Email)
                .HasColumnName("email")
                .IsRequired()
                .HasMaxLength(255);

            // Creamos un índice único para que no se puedan repetir mails en la BD
            builder.HasIndex(u => u.Email).IsUnique();

            builder.Property(u => u.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(u => u.PictureUrl)
                .HasColumnName("picture_url");

            builder.Property(u => u.PasswordHash)
                .HasColumnName("password_hash");

            builder.Property(u => u.Role)
                .HasColumnName("role")
                .IsRequired()
                .HasDefaultValue("Customer");

            builder.Property(u => u.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();
        }
    }
}
