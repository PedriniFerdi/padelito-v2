using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/clients")]
[Authorize(Policy = "AdminOrReception")]
public sealed class ClientsController(ICatalogService catalogService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<ClientListDto>>> Get(CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetClientsAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public Task<ActionResult<ClientDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetClientAsync(id, cancellationToken));
    }

    [HttpPost]
    public Task<ActionResult<ClientDetailDto>> Create(ClientCreateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.CreateClientAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult<ClientDetailDto>> Update(int id, ClientUpdateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.UpdateClientAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:int}/activate")]
    public Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetClientActiveAsync(id, true, cancellationToken));
    }

    [HttpPatch("{id:int}/deactivate")]
    public Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetClientActiveAsync(id, false, cancellationToken));
    }
}
