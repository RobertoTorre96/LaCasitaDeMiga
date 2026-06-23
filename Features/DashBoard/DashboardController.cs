using LaCasitaDeMiga.Features.DashBoard;
using LaCasitaDeMiga.Features.DashBoard.Services;
using Microsoft.AspNetCore.Mvc;

namespace LaCasitaDeMiga.Features.Dashboard {
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardController : ControllerBase {
        private readonly IDashboardService _dashboardService;

        // Corregido: Ahora asignamos la variable correcta
        public DashboardController(IDashboardService dashboardService) {
            _dashboardService = dashboardService;
        }

        // GET: api/dashboard/profits?startDate=2026-06-01&endDate=2026-06-30
        [HttpGet("profits")]
        public async Task<ActionResult<ProfitReportDto>> GetProfits(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate) {

            if (startDate > endDate) {
                return BadRequest(new { message = "La fecha de inicio no puede ser mayor a la fecha de fin." });
            }

            var report = await _dashboardService.GetProfitReportAsync(startDate, endDate);
            return Ok(report);
        }
    }
}