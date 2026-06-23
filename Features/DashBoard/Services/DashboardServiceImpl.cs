using LaCasitaDeMiga.Data;
using LaCasitaDeMiga.Features.Orders;
using Microsoft.EntityFrameworkCore;

namespace LaCasitaDeMiga.Features.DashBoard.Services {
    public class DashboardServiceImpl : IDashboardService {

        private readonly ApplicationDbContext _context;

        public DashboardServiceImpl(ApplicationDbContext context) {
            _context = context;
        }

        public async Task<ProfitReportDto> GetProfitReportAsync(DateTime startDate, DateTime endDate) {
            // Pasamos a UTC para que coincida con cómo graba PostgreSQL en Railway
            var start = startDate.ToUniversalTime();
            var end = endDate.ToUniversalTime();

            // 1. Buscamos los detalles de las órdenes en el rango de fechas (Ignoramos canceladas)
            var items = await _context.OrderItems
                .Where(i => i.Order.CreatedAt >= start && i.Order.CreatedAt <= end)
                .Where(i => i.Order.Status != EOrderStatus.Cancelled)
                .Select(i => new {
                    i.Quantity,
                    i.UnitPrice, // Precio de venta cobrado
                    i.UnitCost   // Costo PPP histórico congelado
                })
                .ToListAsync();

            // 2. HACEMOS LA MATEMÁTICA EN CALIENTE
            decimal totalSales = items.Sum(i => i.Quantity * i.UnitPrice);
            decimal totalCost = items.Sum(i => i.Quantity * i.UnitCost);
            decimal netProfit = totalSales - totalCost; // ◄ GANANCIA PPP CALCULADA ACÁ

            // Calculamos el porcentaje de margen de ganancia
            decimal marginPercentage = totalSales > 0
                ? Math.Round((netProfit / totalSales) * 100, 2)
                : 0;

            // 3. RETORNAMOS EL REPORTE EMPAQUETADO
            return new ProfitReportDto {
                TotalSales = Math.Round(totalSales, 2),
                TotalCost = Math.Round(totalCost, 2),
                NetProfit = Math.Round(netProfit, 2),
                ProfitMarginPercentage = marginPercentage,
                TotalOrdersProcessed = items.Count
            };
        }

    }
}
