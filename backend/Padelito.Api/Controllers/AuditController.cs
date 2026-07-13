using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Audit;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/audit/reservations")]
[Authorize(Policy = "AdminOnly")]
public sealed class AuditController(IAuditService auditService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<ReservationAuditListDto>>> Get([FromQuery] ReservationAuditFilterDto filter, CancellationToken cancellationToken) =>
        HandleAsync(() => auditService.GetReservationAuditsAsync(CurrentClubId, filter, cancellationToken));
}
