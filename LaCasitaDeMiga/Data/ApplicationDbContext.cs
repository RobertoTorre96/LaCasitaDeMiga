using LaCasitaDeMiga.Features.Brands;
using LaCasitaDeMiga.Features.Categories;
using LaCasitaDeMiga.Features.Orders;
using LaCasitaDeMiga.Features.Products;
using LaCasitaDeMiga.Features.Users;
using Microsoft.EntityFrameworkCore;

namespace LaCasitaDeMiga.Data {
    public class ApplicationDbContext :DbContext{

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options){

        }
        public DbSet<CategoryEntity> Categories { get; set; } = null!;
        public DbSet<BrandEntity> Brands { get; set; } = null!;
        public DbSet<ProductEntity> Products { get; set; } = null!;
        public DbSet<ProductVariantEntity> ProductVariants { get; set; } = null!;
        public DbSet<OrderEntity> Orders { get; set; } = null!;
        public DbSet<OrderItemEntity> OrderItems { get; set; } = null!;
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<VariantImageEntity> VariantImages{ get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
