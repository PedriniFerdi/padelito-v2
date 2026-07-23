using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Api.Security;
using Padelito.Application.DTOs.Auth;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AuthController(IAuthService authService, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponseDto>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        if (response is null)
        {
            return Unauthorized();
        }

        Response.Cookies.Append(
            AuthCookie.Name,
            response.Token,
            AuthCookie.CreateOptions(response.ExpiresAt, environment));
        Response.Headers.CacheControl = "no-store";

        return Ok(response);
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var currentUser = await authService.GetCurrentUserAsync(User, cancellationToken);
        if (currentUser is null)
        {
            Response.Cookies.Delete(AuthCookie.Name, AuthCookie.CreateDeleteOptions(environment));
            return Unauthorized();
        }

        Response.Headers.CacheControl = "no-store";
        return Ok(currentUser);
    }

    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(AuthCookie.Name, AuthCookie.CreateDeleteOptions(environment));
        return NoContent();
    }
}
