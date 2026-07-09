using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/available-turns")]
[Authorize(Policy = "AdminOnly")]
public sealed class AvailableTurnsController(ICatalogService catalogService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<AvailableTurnListDto>>> Get(CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetAvailableTurnsAsync(CurrentClubId, cancellationToken));
    }

    [HttpPost]
    public Task<ActionResult<AvailableTurnListDto>> Create(AvailableTurnCreateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.CreateAvailableTurnAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult<AvailableTurnListDto>> Update(int id, AvailableTurnUpdateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.UpdateAvailableTurnAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:int}/activate")]
    public Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetAvailableTurnActiveAsync(id, true, cancellationToken));
    }

    [HttpPatch("{id:int}/deactivate")]
    public Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetAvailableTurnActiveAsync(id, false, cancellationToken));
    }
}
