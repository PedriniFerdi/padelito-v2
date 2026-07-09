using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/court-types")]
[Authorize(Policy = "AdminOnly")]
public sealed class CourtTypesController(ICatalogService catalogService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<CourtTypeDto>>> Get(CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetCourtTypesAsync(cancellationToken));
    }

    [HttpPost]
    public Task<ActionResult<CourtTypeDto>> Create(CourtTypeCreateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.CreateCourtTypeAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult<CourtTypeDto>> Update(int id, CourtTypeUpdateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.UpdateCourtTypeAsync(id, request, cancellationToken));
    }
}
