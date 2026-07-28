using Microsoft.AspNetCore.Identity;
using Padelito.Application.DTOs.Auth;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Security;
using Padelito.Application.Services;
using Padelito.Domain.Entities;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class AuthServiceTests
{
    [Theory]
    [InlineData("Administrador", "Admin")]
    [InlineData("Recepcion", "Reception")]
    [InlineData("Recepción", "Reception")]
    [InlineData("Empleado", "Staff")]
    [InlineData("Admin", "Admin")]
    public async Task Login_normalizes_legacy_role_names(string storedRole, string expectedRole)
    {
        var user = CreateUser(storedRole);
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "admin123");
        var tokenService = new CapturingTokenService();
        var service = new AuthService(
            new UserRepositoryFake(user),
            new PasswordHasher<User>(),
            tokenService);

        var response = await service.LoginAsync(
            new LoginRequestDto { Username = "admin", Password = "admin123" },
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(expectedRole, response.User.Role);
        Assert.Equal(expectedRole, tokenService.User!.Role);
    }

    private static User CreateUser(string roleName)
    {
        return new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = string.Empty,
            EmployeeId = 1,
            RoleId = 1,
            Role = new Role { Id = 1, Name = roleName },
            Employee = new Employee { Id = 1, ClubId = 1 },
            IsActive = true
        };
    }

    private sealed class UserRepositoryFake(User user) : IUserRepository
    {
        public Task<User?> GetByUsernameWithDetailsAsync(string username, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(user);

        public Task<User?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken) =>
            Task.FromResult<User?>(user);
    }

    private sealed class CapturingTokenService : IJwtTokenService
    {
        public CurrentUserDto? User { get; private set; }

        public (string Token, DateTime ExpiresAt) CreateToken(CurrentUserDto user)
        {
            User = user;
            return ("token", DateTime.UtcNow.AddHours(1));
        }
    }
}
