using AutoMapper;
using LaCasitaDeMiga.Common.DTOs;
using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Exceptions;
using LaCasitaDeMiga.Features.Products;
using LaCasitaDeMiga.Features.Products.DTOs;
using LaCasitaDeMiga.Features.Products.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace LaCasitaDeMiga.Tests.Features.Products.Services {
    public class ProductServiceImplTests {
        // =========================================================================
        // ⚙️ CONFIGURACIONES Y AYUDANTES
        // =========================================================================
        private IMapper CreateRealMapper() {
            var config = new MapperConfiguration(cfg => {
                // Configuramos los mapeos mínimos necesarios para que GetById / Create no devuelvan nulo
                cfg.CreateMap<ProducCreatetRequestDto, ProductEntity>();
                cfg.CreateMap<ProductEntity, ProductResponseDto>();
                cfg.CreateMap<ProductVariantEntity, ProductVariantResponseDto>();
            });
            return config.CreateMapper();
        }

        // =========================================================================
        // 📦 PRUEBAS: RegisterStockEntryAsync (Fórmula de Costo Ponderado)
        // =========================================================================

        [Theory]
        [InlineData(0)]
        [InlineData(-5)]
        public async Task RegisterStockEntryAsync_WhenQuantityIsLessThanOrEqualToCero_ShouldThrowBadRequestException(int invalidQuantity) {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: $"Stock_InvalidQty_{invalidQuantity}")
                .Options;

            using var context = new ApplicationDbContext(options);
            var service = new ProductServiceImpl(context, new Mock<IMapper>().Object);

            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                service.RegisterStockEntryAsync(Guid.NewGuid(), invalidQuantity, 150.00m)
            );

            Assert.Equal("La cantidad recibida debe ser mayor a 0.", exception.Message);
        }

        [Fact]
        public async Task RegisterStockEntryAsync_WhenPriceIsNegative_ShouldThrowBadRequestException() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Stock_NegativePrice")
                .Options;

            using var context = new ApplicationDbContext(options);
            var service = new ProductServiceImpl(context, new Mock<IMapper>().Object);

            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                service.RegisterStockEntryAsync(Guid.NewGuid(), 10, -10.50m)
            );

            Assert.Equal("El precio de compra no puede ser negativo.", exception.Message);
        }

        [Fact]
        public async Task RegisterStockEntryAsync_WhenVariantDoesNotExist_ShouldThrowNotFoundException() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Stock_VariantNotFound")
                .Options;

            using var context = new ApplicationDbContext(options);
            var service = new ProductServiceImpl(context, new Mock<IMapper>().Object);
            var randomId = Guid.NewGuid();

            var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
                service.RegisterStockEntryAsync(randomId, 10, 100m)
            );

            Assert.Equal($"La variante con ID: '{randomId}' no fue encontrada.", exception.Message);
        }

        [Fact]
        public async Task RegisterStockEntryAsync_WhenValidEntry_ShouldCalculateAverageCostAndIncrementVersion() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Stock_Success_Calculations")
                .Options;

            var variantId = Guid.NewGuid();

            using var context = new ApplicationDbContext(options);
            context.ProductVariants.Add(new ProductVariantEntity {
                Id = variantId,
                Sku = "PAN-CASERO-V1",
                Stock = 10,             // Stock Inicial
                AverageCost = 100.00m,  // Costo Promedio Inicial
                Version = 1             // Versión de concurrencia inicial
            });
            await context.SaveChangesAsync();

            var service = new ProductServiceImpl(context, new Mock<IMapper>().Object);

            // ACT -> Entran 5 unidades nuevas a un precio de compra de $200.00 cada una
            // Fórmula Esperada: ((10 * 100) + (5 * 200)) / (10 + 5) = (1000 + 1000) / 15 = 2000 / 15 = 133.3333... -> Redondeado a 133.33
            var result = await service.RegisterStockEntryAsync(variantId, 5, 200.00m);

            // ASSERT
            Assert.True(result);

            var updatedVariant = await context.ProductVariants.FindAsync(variantId);
            Assert.Equal(15, updatedVariant!.Stock); // 10 originales + 5 nuevos = 15
            Assert.Equal(133.33m, updatedVariant.AverageCost); // Verificación del Costo Promedio Ponderado
            Assert.Equal(200.00m, updatedVariant.LastPurchasePrice); // Cambia al último precio
            Assert.Equal(2, updatedVariant.Version); // Se incrementó la versión de concurrencia
        }

        // =========================================================================
        // 📊 PRUEBAS: UpdateStockAsync (Ajustes de Inventario manuales)
        // =========================================================================

        [Fact]
        public async Task UpdateStockAsync_WhenNegativeAdjustmentExceedsAvailableStock_ShouldThrowBadRequestException() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Stock_Adjustment_Insufficent")
                .Options;

            var variantId = Guid.NewGuid();

            using var context = new ApplicationDbContext(options);
            context.ProductVariants.Add(new ProductVariantEntity {
                Id = variantId,
                Sku = "GALLETA-CHIPS",
                Stock = 3 // Solo hay 3 unidades en el sistema
            });
            await context.SaveChangesAsync();

            var service = new ProductServiceImpl(context, new Mock<IMapper>().Object);

            // ACT -> Intentamos restar 5 unidades (lo que dejaría el stock en -2)
            var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
                service.UpdateStockAsync(variantId, -5)
            );

            Assert.Contains("No hay stock suficiente para el SKU GALLETA-CHIPS.", exception.Message);
        }

        [Fact]
        public async Task UpdateStockAsync_WhenValidAdjustment_ShouldModifyStockAndIncrementVersion() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Stock_Adjustment_Success")
                .Options;

            var variantId = Guid.NewGuid();

            using var context = new ApplicationDbContext(options);
            context.ProductVariants.Add(new ProductVariantEntity { Id = variantId, Sku = "DONA-GLA", Stock = 20, Version = 1 });
            await context.SaveChangesAsync();

            var service = new ProductServiceImpl(context, new Mock<IMapper>().Object);

            // Restamos 5 unidades de forma válida
            var result = await service.UpdateStockAsync(variantId, -5);

            Assert.True(result);
            var variant = await context.ProductVariants.FindAsync(variantId);
            Assert.Equal(15, variant!.Stock);
            Assert.Equal(2, variant.Version);
        }

        // =========================================================================
        // 📑 PRUEBAS: GetAllAsync (Sanitización y Paginación)
        // =========================================================================

        [Fact]
        public async Task GetAllAsync_WhenPaginationParamsAreOutofBounds_ShouldApplySanitization() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Products_GetAll_Sanitization")
                .Options;

            using var context = new ApplicationDbContext(options);
            // El mapeador real es necesario para procesar el mapeo de listas del GetAll
            var service = new ProductServiceImpl(context, CreateRealMapper());

            // Pasamos parámetros locos: página 0 y tamaño de página de 200
            var result = await service.GetAllAsync(pageNumber: 0, pageSize: 200);

            Assert.Equal(1, result.PageNumber); // Corregido a un mínimo de 1
            Assert.Equal(50, result.PageSize);  // Capeado a un máximo de 50
        }

        // =========================================================================
        // 🔍 PRUEBAS: GetByIdAsync / GetBySlugAsync
        // =========================================================================

        [Fact]
        public async Task GetByIdAsync_WhenProductDoesNotExist_ShouldThrowNotFoundException() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "Products_GetById_NotFound")
                .Options;

            using var context = new ApplicationDbContext(options);
            var service = new ProductServiceImpl(context, new Mock<IMapper>().Object);

            var exception = await Assert.ThrowsAsync<NotFoundException>(() =>
                service.GetByIdAsync(Guid.NewGuid())
            );

            Assert.Equal("Producto no encontrado", exception.Message);
        }
    }
}