namespace LaCasitaDeMiga.Features.DashBoard.Services {
    public interface IDashboardService {
        Task<ProfitReportDto> GetProfitReportAsync(DateTime startDate, DateTime endDate);
    }
}
