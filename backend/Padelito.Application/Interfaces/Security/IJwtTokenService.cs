using Padelito.Application.DTOs.Auth;

namespace Padelito.Application.Interfaces.Security;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(CurrentUserDto user);
}
