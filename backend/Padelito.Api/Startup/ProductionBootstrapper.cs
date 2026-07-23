using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Api.Startup;

public static partial class ProductionBootstrapper
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("Bootstrap:Enabled"))
        {
            return;
        }

        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();

        if (await dbContext.Users.AnyAsync(cancellationToken))
        {
            logger.LogWarning("Bootstrap is enabled but users already exist. No bootstrap data was created; disable Bootstrap__Enabled.");
            return;
        }

        var values = ReadAndValidate(configuration);
        var adminRole = await dbContext.Roles.SingleOrDefaultAsync(x => x.Name == "Administrador", cancellationToken)
            ?? throw new InvalidOperationException("The Administrador role is missing. Apply database migrations before enabling bootstrap.");

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var club = new Club
        {
            Name = values.ClubName,
            Email = values.AdminEmail,
            IsActive = true,
            CreatedAt = now
        };
        var person = new Person
        {
            FirstName = values.AdminFirstName,
            LastName = values.AdminLastName,
            Dni = values.AdminDni,
            Phone = values.AdminPhone,
            Email = values.AdminEmail,
            IsActive = true,
            CreatedAt = now
        };

        dbContext.AddRange(club, person);
        await dbContext.SaveChangesAsync(cancellationToken);

        var employee = new Employee { ClubId = club.Id, PersonId = person.Id };
        dbContext.Employees.Add(employee);
        await dbContext.SaveChangesAsync(cancellationToken);

        var user = new User
        {
            Username = values.AdminUsername,
            PasswordHash = string.Empty,
            EmployeeId = employee.Id,
            RoleId = adminRole.Id,
            IsActive = true,
            CreatedAt = now
        };
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        user.PasswordHash = passwordHasher.HashPassword(user, values.AdminPassword);
        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        logger.LogInformation("Production bootstrap created the initial club and administrator. Disable bootstrap and remove its password now.");
    }

    private static BootstrapValues ReadAndValidate(IConfiguration configuration)
    {
        string Required(string key, int maxLength)
        {
            var value = configuration[key]?.Trim();
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException($"Bootstrap setting '{key}' is required when bootstrap is enabled.");
            }

            return value.Length > maxLength
                ? throw new InvalidOperationException($"Bootstrap setting '{key}' cannot exceed {maxLength} characters.")
                : value;
        }

        var password = Required("Bootstrap:AdminPassword", 100);
        if (password.Length < 12)
        {
            throw new InvalidOperationException("Bootstrap administrator password must contain at least 12 characters.");
        }

        var email = Required("Bootstrap:AdminEmail", 120).ToLowerInvariant();
        if (!email.Contains('@', StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Bootstrap administrator email is invalid.");
        }

        var dni = NonDigitsRegex().Replace(Required("Bootstrap:AdminDni", 20), string.Empty);
        if (dni.Length is < 7 or > 10)
        {
            throw new InvalidOperationException("Bootstrap administrator DNI is invalid.");
        }

        return new BootstrapValues(
            Required("Bootstrap:ClubName", 120),
            Required("Bootstrap:AdminUsername", 50),
            password,
            Required("Bootstrap:AdminFirstName", 60),
            Required("Bootstrap:AdminLastName", 60),
            dni,
            Required("Bootstrap:AdminPhone", 40),
            email);
    }

    [GeneratedRegex("[^0-9]")]
    private static partial Regex NonDigitsRegex();

    private sealed record BootstrapValues(
        string ClubName,
        string AdminUsername,
        string AdminPassword,
        string AdminFirstName,
        string AdminLastName,
        string AdminDni,
        string AdminPhone,
        string AdminEmail);
}
