using System.Security.Claims;
using Padelito.Application.DTOs.Auth;

namespace Padelito.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken);
    Task<CurrentUserDto?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
}
