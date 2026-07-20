using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Padelito.Application.DTOs.Auth;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Security;
using Padelito.Application.Interfaces.Services;
using Padelito.Domain.Entities;

namespace Padelito.Application.Services;

public sealed class AuthService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<AuthResponseDto?> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Username) || request.Username.Length > 50 || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var username = request.Username.Trim();

        var user = await userRepository.GetByUsernameWithDetailsAsync(username, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var currentUser = ToCurrentUser(user);
        var token = jwtTokenService.CreateToken(currentUser);

        return new AuthResponseDto
        {
            Token = token.Token,
            ExpiresAt = token.ExpiresAt,
            User = currentUser
        };
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var userIdValue = principal.FindFirstValue("UserId");
        if (!int.TryParse(userIdValue, out var userId))
        {
            return null;
        }

        var user = await userRepository.GetByIdWithDetailsAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        return ToCurrentUser(user);
    }

    private static CurrentUserDto ToCurrentUser(User user)
    {
        return new CurrentUserDto
        {
            UserId = user.Id,
            Username = user.Username,
            EmployeeId = user.EmployeeId,
            Role = user.Role.Name,
            ClubId = user.Employee.ClubId
        };
    }
}
