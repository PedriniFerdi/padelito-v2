using System.Data;
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
        var values = ReadAndValidate(configuration);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var existingUser = await dbContext.Users
            .Include(x => x.Role)
            .Include(x => x.Employee)
                .ThenInclude(x => x.Club)
            .Include(x => x.Employee)
                .ThenInclude(x => x.Person)
            .SingleOrDefaultAsync(x => x.Username == values.AdminUsername, cancellationToken);

        if (existingUser is not null && await IsExactCompletedBootstrapAsync(dbContext, existingUser, values, cancellationToken))
        {
            await transaction.CommitAsync(cancellationToken);
            logger.LogWarning(
                "Bootstrap is still enabled for the already provisioned club {ClubId} and administrator {UserId}. Disable Bootstrap__Enabled and remove its password.",
                existingUser.Employee.ClubId,
                existingUser.Id);
            return;
        }

        if (await HasApplicationDataAsync(dbContext, cancellationToken))
        {
            throw new InvalidOperationException(
                "Bootstrap requires an empty application database or the exact previously bootstrapped administrator graph. No data was changed.");
        }

        var adminRole = await dbContext.Roles.SingleOrDefaultAsync(x => x.Name == "Admin", cancellationToken)
            ?? throw new InvalidOperationException("The Admin role is missing. Apply database migrations before enabling bootstrap.");

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

    private static async Task<bool> IsExactCompletedBootstrapAsync(
        PadelitoDbContext dbContext,
        User user,
        BootstrapValues values,
        CancellationToken cancellationToken)
    {
        var person = user.Employee.Person;
        var club = user.Employee.Club;

        if (!user.IsActive
            || user.Role.Name != "Admin"
            || !club.IsActive
            || !string.Equals(club.Name, values.ClubName, StringComparison.Ordinal)
            || !string.Equals(club.Email, values.AdminEmail, StringComparison.OrdinalIgnoreCase)
            || !person.IsActive
            || !string.Equals(person.FirstName, values.AdminFirstName, StringComparison.Ordinal)
            || !string.Equals(person.LastName, values.AdminLastName, StringComparison.Ordinal)
            || !string.Equals(person.Dni, values.AdminDni, StringComparison.Ordinal)
            || !string.Equals(person.Phone, values.AdminPhone, StringComparison.Ordinal)
            || !string.Equals(person.Email, values.AdminEmail, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return await dbContext.Clubs.CountAsync(cancellationToken) == 1
            && await dbContext.People.CountAsync(cancellationToken) == 1
            && await dbContext.Employees.CountAsync(cancellationToken) == 1
            && await dbContext.Users.CountAsync(cancellationToken) == 1
            && !await dbContext.Clients.AnyAsync(cancellationToken)
            && !await dbContext.Courts.AnyAsync(cancellationToken)
            && !await dbContext.AvailableTurns.AnyAsync(cancellationToken)
            && !await dbContext.Promotions.AnyAsync(cancellationToken)
            && !await dbContext.Reservations.AnyAsync(cancellationToken)
            && !await dbContext.Payments.AnyAsync(cancellationToken)
            && !await dbContext.ReservationAudits.AnyAsync(cancellationToken);
    }

    private static async Task<bool> HasApplicationDataAsync(
        PadelitoDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await dbContext.Clubs.AnyAsync(cancellationToken)
            || await dbContext.People.AnyAsync(cancellationToken)
            || await dbContext.Clients.AnyAsync(cancellationToken)
            || await dbContext.Employees.AnyAsync(cancellationToken)
            || await dbContext.Users.AnyAsync(cancellationToken)
            || await dbContext.Courts.AnyAsync(cancellationToken)
            || await dbContext.AvailableTurns.AnyAsync(cancellationToken)
            || await dbContext.Promotions.AnyAsync(cancellationToken)
            || await dbContext.Reservations.AnyAsync(cancellationToken)
            || await dbContext.Payments.AnyAsync(cancellationToken)
            || await dbContext.ReservationAudits.AnyAsync(cancellationToken);
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
            throw new InvalidOperationException("Bootstrap administrator Customer ID is invalid.");
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
