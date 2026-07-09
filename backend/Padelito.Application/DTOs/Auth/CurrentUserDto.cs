namespace Padelito.Application.DTOs.Auth;

public sealed class CurrentUserDto
{
    public int UserId { get; init; }
    public required string Username { get; init; }
    public int EmployeeId { get; init; }
    public required string Role { get; init; }
    public int ClubId { get; init; }
}
