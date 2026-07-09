using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Catalogs;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class UsersController(ICatalogService catalogService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<UserListDto>>> Get(CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetUsersAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public Task<ActionResult<UserDetailDto>> GetById(int id, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.GetUserAsync(id, cancellationToken));
    }

    [HttpPost]
    public Task<ActionResult<UserDetailDto>> Create(UserCreateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.CreateUserAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public Task<ActionResult<UserDetailDto>> Update(int id, UserUpdateDto request, CancellationToken cancellationToken)
    {
        return HandleAsync(() => catalogService.UpdateUserAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:int}/change-password")]
    public Task<IActionResult> ChangePassword(int id, ChangePasswordDto request, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.ChangeUserPasswordAsync(id, request, cancellationToken));
    }

    [HttpPatch("{id:int}/activate")]
    public Task<IActionResult> Activate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetUserActiveAsync(id, true, cancellationToken));
    }

    [HttpPatch("{id:int}/deactivate")]
    public Task<IActionResult> Deactivate(int id, CancellationToken cancellationToken)
    {
        return HandleNoContentAsync(() => catalogService.SetUserActiveAsync(id, false, cancellationToken));
    }
}
