using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/employees")]
[Authorize(Policy = "AdminOnly")]
public sealed class EmployeesController(ICatalogService catalogService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<EmployeeListDto>>> Get(CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetEmployeesAsync(CurrentClubId, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public Task<ActionResult<EmployeeDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetEmployeeAsync(id, cancellationToken));
    }

    [HttpPost]
    public Task<ActionResult<EmployeeDetailDto>> Create(EmployeeCreateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.CreateEmployeeAsync(CurrentClubId, request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult<EmployeeDetailDto>> Update(int id, EmployeeUpdateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.UpdateEmployeeAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:int}/activate")]
    public Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetEmployeeActiveAsync(id, true, cancellationToken));
    }

    [HttpPatch("{id:int}/deactivate")]
    public Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetEmployeeActiveAsync(id, false, cancellationToken));
    }
}
