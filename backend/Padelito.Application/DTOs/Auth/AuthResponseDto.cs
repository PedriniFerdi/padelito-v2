namespace Padelito.Application.DTOs.Auth;

public sealed class AuthResponseDto
{
    public required string Token { get; init; }
    public DateTime ExpiresAt { get; init; }
    public required CurrentUserDto User { get; init; }
}
