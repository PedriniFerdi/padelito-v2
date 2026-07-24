using System.ComponentModel.DataAnnotations;

namespace Padelito.Application.DTOs.Auth;

public sealed class LoginRequestDto
{
    [Required(ErrorMessage = "Username is required.")]
    [StringLength(50, ErrorMessage = "Username cannot exceed 50 characters.")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "Password is required.")]
    public required string Password { get; init; }
}
