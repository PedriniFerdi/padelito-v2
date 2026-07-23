using System.Text.Json.Serialization;

namespace Padelito.Application.DTOs.Auth;

public sealed class AuthResponseDto
{
    [JsonIgnore]
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public required CurrentUserDto User { get; init; }
}
