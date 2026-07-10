using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.Common;

namespace Padelito.Api.Controllers;

public abstract class CatalogControllerBase : ControllerBase
{
    protected int CurrentClubId
    {
        get
        {
            var claimValue = User.FindFirstValue("ClubId");
            return int.TryParse(claimValue, out var clubId)
                ? clubId
                : throw new UnauthorizedAccessException("El token no contiene un club válido.");
        }
    }

    protected ActionResult<T> Handle<T>(Func<T> action)
    {
        try
        {
            return Ok(action());
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (ConflictException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    protected async Task<ActionResult<T>> HandleAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Ok(await action());
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (ConflictException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }

    protected async Task<IActionResult> HandleNoContentAsync(Func<Task> action)
    {
        try
        {
            await action();
            return NoContent();
        }
        catch (BusinessException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
        catch (ConflictException exception)
        {
            return Conflict(new { message = exception.Message });
        }
    }
}
