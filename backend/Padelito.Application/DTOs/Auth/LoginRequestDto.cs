using System.ComponentModel.DataAnnotations;

namespace Padelito.Application.DTOs.Auth;

public sealed class LoginRequestDto
{
    [Required(ErrorMessage = "El usuario es obligatorio.")]
    [StringLength(50, ErrorMessage = "El usuario no puede superar los 50 caracteres.")]
    public required string Username { get; init; }

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    public required string Password { get; init; }
}
