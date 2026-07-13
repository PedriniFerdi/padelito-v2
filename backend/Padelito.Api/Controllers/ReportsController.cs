using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Reports;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/reports/reservations")]
[Authorize(Policy = "AdminOrReception")]
public sealed class ReportsController(IReportService reportService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<ReservationReportDto>> Get([FromQuery] ReservationReportFilterDto filter, CancellationToken cancellationToken) =>
        HandleAsync(() => reportService.GetReservationsAsync(CurrentClubId, filter, cancellationToken));

    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] ReservationReportFilterDto filter, CancellationToken cancellationToken)
    {
        try
        {
            var content = await reportService.ExportReservationsCsvAsync(CurrentClubId, filter, cancellationToken);
            return File(content, "text/csv; charset=utf-8", $"reporte-reservas-{DateTime.UtcNow:yyyyMMdd}.csv");
        }
        catch (Application.Common.BusinessException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }
}
