using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/courts")]
[Authorize(Policy = "AdminOnly")]
public sealed class CourtsController(ICatalogService catalogService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<CourtListDto>>> Get(CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetCourtsAsync(CurrentClubId, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public Task<ActionResult<CourtDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetCourtAsync(id, cancellationToken));
    }

    [HttpPost]
    public Task<ActionResult<CourtDetailDto>> Create(CourtCreateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.CreateCourtAsync(CurrentClubId, request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult<CourtDetailDto>> Update(int id, CourtUpdateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.UpdateCourtAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:int}/activate")]
    public Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetCourtActiveAsync(id, true, cancellationToken));
    }

    [HttpPatch("{id:int}/deactivate")]
    public Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetCourtActiveAsync(id, false, cancellationToken));
    }
}
