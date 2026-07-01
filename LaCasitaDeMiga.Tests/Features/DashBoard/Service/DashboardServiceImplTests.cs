using Xunit;
using Microsoft.EntityFrameworkCore;
using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Features.Orders;
using LaCasitaDeMiga.Features.DashBoard;
using LaCasitaDeMiga.Features.DashBoard.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LaCasitaDeMiga.Tests.Features.DashBoard.Services {
    public class DashboardServiceImplTests {
        // 📦 Función ayudante para generar una base de datos limpia y aislada por test
        private ApplicationDbContext CreateInMemoryDbContext() {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new ApplicationDbContext(options);
        }

        // =========================================================================
        // 📊 PRUEBAS: GetProfitReportAsync (Lógica Financiera y Filtros)
        // =========================================================================

        [Fact]
        public async Task GetProfitReportAsync_WhenOrdersAreCancelled_ShouldIgnoreThemInReport() {
            using var context = CreateInMemoryDbContext();

            var startDate = new DateTime(2026, 06, 01);
            var endDate = new DateTime(2026, 06, 30);

            // Creamos una orden CANCELADA dentro del rango de fechas
            var cancelledOrder = new OrderEntity {
                Id = Guid.NewGuid(),
                CreatedAt = new DateTime(2026, 06, 15),
                Status = EOrderStatus.Cancelled // Cancelada
            };

            context.OrderItems.Add(new OrderItemEntity {
                Id = Guid.NewGuid(),
                Order = cancelledOrder,
                Quantity = 10,
                UnitPrice = 500m,
                UnitCost = 200m
            });

            await context.SaveChangesAsync();

            var service = new DashboardServiceImpl(context);

            // ACT: Pedimos el reporte
            var result = await service.GetProfitReportAsync(startDate, endDate);

            // ASSERT: Al estar cancelada, todo debe dar 0 y procesar 0 órdenes
            Assert.Equal(0, result.TotalOrdersProcessed);
            Assert.Equal(0m, result.TotalSales);
            Assert.Equal(0m, result.NetProfit);
        }

        [Fact]
        public async Task GetProfitReportAsync_WhenDataIsValid_ShouldCalculateCorrectFinancialMaths() {
            using var context = CreateInMemoryDbContext();

            var startDate = new DateTime(2026, 06, 01);
            var endDate = new DateTime(2026, 06, 30);

            // 1. Orden Válida (Dentro del rango)
            var validOrder = new OrderEntity {
                Id = Guid.NewGuid(),
                CreatedAt = new DateTime(2026, 06, 10),
                Status = EOrderStatus.Completed
            };

            // Ítem 1: 2 unidades vendidas a $1000 c/u, con costo PPP de $400
            // Venta = 2000, Costo = 800
            context.OrderItems.Add(new OrderItemEntity { Order = validOrder, Quantity = 2, UnitPrice = 1000m, UnitCost = 400m });

            // Ítem 2: 1 unidad vendida a $5000 c/u, con costo PPP de $3000
            // Venta = 5000, Costo = 3000
            context.OrderItems.Add(new OrderItemEntity { Order = validOrder, Quantity = 1, UnitPrice = 5000m, UnitCost = 3000m });

            // 2. Orden Fuera de Rango (Debe ser ignorada por fecha)
            var outOfRangeOrder = new OrderEntity {
                Id = Guid.NewGuid(),
                CreatedAt = new DateTime(2026, 07, 05), // Julio (Fuera de rango)
                Status = EOrderStatus.Completed
            };
            context.OrderItems.Add(new OrderItemEntity { Order = outOfRangeOrder, Quantity = 5, UnitPrice = 2000m, UnitCost = 1000m });

            await context.SaveChangesAsync();

            var service = new DashboardServiceImpl(context);

            // ACT
            var result = await service.GetProfitReportAsync(startDate, endDate);

            // MATEMÁTICAS ESPERADAS (Solo de la orden válida):
            // TotalSales = 2000 + 5000 = 7000
            // TotalCost  = 800 + 3000 = 3800
            // NetProfit  = 7000 - 3800 = 3200
            // Margin %   = (3200 / 7000) * 100 = 45.714... -> Redondeado a 45.71

            // ASSERT
            Assert.Equal(2, result.TotalOrdersProcessed); // 2 ítems procesados dentro de esa fecha
            Assert.Equal(7000m, result.TotalSales);
            Assert.Equal(3800m, result.TotalCost);
            Assert.Equal(3200m, result.NetProfit);
            Assert.Equal(45.71m, result.ProfitMarginPercentage);
        }

        [Fact]
        public async Task GetProfitReportAsync_WhenNoSalesExist_ShouldReturnZeroBalancesWithoutDividingByZero() {
            using var context = CreateInMemoryDbContext();
            var service = new DashboardServiceImpl(context);

            // ACT: Pedimos reporte sobre una base de datos vacía
            var result = await service.GetProfitReportAsync(DateTime.UtcNow.AddDays(-5), DateTime.UtcNow);

            // ASSERT: Protegemos el cortocircuito 'totalSales > 0 ? ... : 0' para evitar error de división por cero
            Assert.Equal(0m, result.TotalSales);
            Assert.Equal(0m, result.ProfitMarginPercentage);
            Assert.Equal(0, result.TotalOrdersProcessed);
        }
    }
}