using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Dashboard;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize(Policy = "AuthenticatedStaff")]
public sealed class DashboardController(IDashboardService dashboardService) : CatalogControllerBase
{
    [HttpGet("summary")]
    public Task<ActionResult<DashboardSummaryDto>> Get(CancellationToken cancellationToken) =>
        HandleAsync(() => dashboardService.GetSummaryAsync(CurrentClubId, cancellationToken));
}

