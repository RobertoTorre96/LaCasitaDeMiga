namespace LaCasitaDeMiga.Features.DashBoard {
    public class ProfitReportDto {
        public decimal TotalSales { get; set; }             // Facturación Bruta (Ingresos)
        public decimal TotalCost { get; set; }              // Costo de Mercadería Vendida (Costos PPP)
        public decimal NetProfit { get; set; }              // Ganancia Neta Real (Plata limpia)
        public decimal ProfitMarginPercentage { get; set; }     // Porcentaje de margen (Ej: 45%)
        public int TotalOrdersProcessed { get; set; }         // Cantidad de ventas realizadas
    }
}
