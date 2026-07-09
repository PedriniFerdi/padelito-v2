using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/promotions")]
[Authorize(Policy = "AdminOnly")]
public sealed class PromotionsController(ICatalogService catalogService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<PromotionListDto>>> Get(CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetPromotionsAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public Task<ActionResult<PromotionListDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetPromotionAsync(id, cancellationToken));
    }

    [HttpPost]
    public Task<ActionResult<PromotionListDto>> Create(PromotionCreateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.CreatePromotionAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult<PromotionListDto>> Update(int id, PromotionUpdateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.UpdatePromotionAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:int}/activate")]
    public Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetPromotionActiveAsync(id, true, cancellationToken));
    }

    [HttpPatch("{id:int}/deactivate")]
    public Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetPromotionActiveAsync(id, false, cancellationToken));
    }
}
