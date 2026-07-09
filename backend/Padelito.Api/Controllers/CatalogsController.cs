using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/catalogs")]
[Authorize(Policy = "AuthenticatedStaff")]
public sealed class CatalogsController(ICatalogService catalogService) : CatalogControllerBase
{
    [HttpGet("roles")]
    [Authorize(Policy = "AdminOnly")]
    public Task<ActionResult<IReadOnlyList<RoleDto>>> GetRoles(CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetRolesAsync(cancellationToken));
    }
}
