using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Api.Startup;

public static class DemoAccessProvisioner
{
    public static async Task InitializeAsync(
        IServiceProvider services,
        IConfiguration configuration,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        if (!configuration.GetValue<bool>("DemoAccessProvisioning:Enabled"))
        {
            return;
        }

        var values = ReadAndValidate(configuration);
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var clubs = await dbContext.Clubs.Where(x => x.IsActive).ToListAsync(cancellationToken);
        if (clubs.Count != 1 || await dbContext.Clubs.CountAsync(cancellationToken) != 1)
        {
            throw new InvalidOperationException("Demo access provisioning requires exactly one active club.");
        }

        var adminRole = await dbContext.Roles.SingleOrDefaultAsync(x => x.Name == "Admin", cancellationToken)
            ?? throw new InvalidOperationException("The Admin role is missing.");
        var receptionRole = await dbContext.Roles.SingleOrDefaultAsync(x => x.Name == "Reception", cancellationToken)
            ?? throw new InvalidOperationException("The Reception role is missing.");

        var demoUsernames = new[] { values.AdminUsername, values.ReceptionUsername };
        var demoUsers = await dbContext.Users
            .Include(x => x.Role)
            .Where(x => demoUsernames.Contains(x.Username))
            .ToListAsync(cancellationToken);
        if (demoUsers.Count(x => x.Username == values.AdminUsername) != 1)
        {
            throw new InvalidOperationException("The configured public Admin username must already exist exactly once.");
        }

        if (demoUsers.All(x => x.Username != values.ReceptionUsername))
        {
            var receptionPasswordHash = ValidateReceptionPasswordHash(values.ReceptionPasswordHash);
            var receptionEmployee = await dbContext.Employees
                .Include(x => x.Person)
                .Include(x => x.User)
                .SingleOrDefaultAsync(
                    x => x.PersonId == values.ReceptionPersonId
                        && x.ClubId == clubs[0].Id,
                    cancellationToken)
                ?? throw new InvalidOperationException(
                    "The configured Reception employee fingerprint does not exist.");

            if (receptionEmployee.User is not null
                || !receptionEmployee.Person.IsActive
                || receptionEmployee.Person.FirstName != values.ReceptionFirstName
                || receptionEmployee.Person.LastName != values.ReceptionLastName)
            {
                throw new InvalidOperationException(
                    "The configured Reception employee is inactive, already linked, or has an unexpected identity.");
            }

            var receptionUser = new User
            {
                Username = values.ReceptionUsername,
                PasswordHash = receptionPasswordHash,
                EmployeeId = receptionEmployee.Id,
                RoleId = receptionRole.Id,
                IsActive = true,
                IsDemo = true,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Users.Add(receptionUser);
            demoUsers.Add(receptionUser);
        }

        if (demoUsers.Count != demoUsernames.Length)
        {
            throw new InvalidOperationException("The configured public demo usernames are not unique.");
        }

        foreach (var (username, expectedRole) in new[]
        {
            (values.AdminUsername, adminRole.Name),
            (values.ReceptionUsername, receptionRole.Name)
        })
        {
            var user = demoUsers.Single(x => x.Username == username);
            if (!user.IsActive || !user.Role.Name.Equals(expectedRole, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Public demo user '{username}' is inactive or has an unexpected role.");
            }
        }

        var privateUser = await dbContext.Users
            .Include(x => x.Role)
            .Include(x => x.Employee).ThenInclude(x => x.Person)
            .SingleOrDefaultAsync(x => x.Username == values.PrivateUsername, cancellationToken);

        if (privateUser is null)
        {
            if (await dbContext.People.AnyAsync(x => x.Dni == values.Dni, cancellationToken))
            {
                throw new InvalidOperationException("The private administrator Customer ID is already in use.");
            }

            var now = DateTime.UtcNow;
            var person = new Person
            {
                FirstName = values.FirstName,
                LastName = values.LastName,
                Dni = values.Dni,
                Phone = values.Phone,
                Email = values.Email,
                IsActive = true,
                CreatedAt = now
            };
            var employee = new Employee { ClubId = clubs[0].Id, Person = person };
            privateUser = new User
            {
                Username = values.PrivateUsername,
                PasswordHash = string.Empty,
                Employee = employee,
                RoleId = adminRole.Id,
                IsActive = true,
                IsDemo = false,
                CreatedAt = now
            };
            privateUser.PasswordHash = passwordHasher.HashPassword(privateUser, values.PrivatePassword);
            dbContext.Users.Add(privateUser);
        }
        else if (!IsExactPrivateAdministrator(privateUser, clubs[0].Id, adminRole.Id, values, passwordHasher))
        {
            throw new InvalidOperationException(
                "The configured private administrator already exists but does not match the expected protected profile.");
        }

        foreach (var demoUser in demoUsers)
        {
            demoUser.IsDemo = true;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        logger.LogWarning(
            "Demo access provisioning completed for {DemoUserCount} public accounts and private administrator {PrivateUserId}. Disable provisioning and remove its password secret.",
            demoUsers.Count,
            privateUser.Id);
    }

    private static bool IsExactPrivateAdministrator(
        User user,
        int clubId,
        int adminRoleId,
        ProvisioningValues values,
        IPasswordHasher<User> passwordHasher)
    {
        var person = user.Employee.Person;
        return user.IsActive
            && !user.IsDemo
            && user.RoleId == adminRoleId
            && user.Employee.ClubId == clubId
            && person.IsActive
            && person.FirstName == values.FirstName
            && person.LastName == values.LastName
            && person.Dni == values.Dni
            && person.Phone == values.Phone
            && person.Email == values.Email
            && passwordHasher.VerifyHashedPassword(user, user.PasswordHash, values.PrivatePassword)
                != PasswordVerificationResult.Failed;
    }

    private static ProvisioningValues ReadAndValidate(IConfiguration configuration)
    {
        string Value(string key, string fallback, int maxLength)
        {
            var value = (configuration[key] ?? fallback).Trim();
            if (string.IsNullOrWhiteSpace(value) || value.Length > maxLength)
            {
                throw new InvalidOperationException($"Demo access setting '{key}' is invalid.");
            }

            return value;
        }

        var password = configuration["DemoAccessProvisioning:PrivatePassword"];
        if (string.IsNullOrWhiteSpace(password) || password.Length is < 12 or > 100)
        {
            throw new InvalidOperationException(
                "Demo access private administrator password must contain between 12 and 100 characters.");
        }

        var adminUsername = Value("DemoAccessProvisioning:AdminUsername", "admin", 50);
        var receptionUsername = Value("DemoAccessProvisioning:ReceptionUsername", "juanperez", 50);
        if (adminUsername.Equals(receptionUsername, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The public Admin and Reception usernames must be different.");
        }

        if (!int.TryParse(configuration["DemoAccessProvisioning:ReceptionPersonId"] ?? "3", out var receptionPersonId)
            || receptionPersonId <= 0)
        {
            throw new InvalidOperationException(
                "Demo access setting 'DemoAccessProvisioning:ReceptionPersonId' is invalid.");
        }

        return new ProvisioningValues(
            adminUsername,
            receptionUsername,
            receptionPersonId,
            Value("DemoAccessProvisioning:ReceptionFirstName", "Juan", 60),
            Value("DemoAccessProvisioning:ReceptionLastName", "Perez", 60),
            configuration["DemoAccessProvisioning:ReceptionPasswordHash"]?.Trim(),
            Value("DemoAccessProvisioning:PrivateUsername", "Ferdi", 50),
            password,
            Value("DemoAccessProvisioning:FirstName", "Ferdi", 60),
            Value("DemoAccessProvisioning:LastName", "Admin", 60),
            Value("DemoAccessProvisioning:Dni", "99000001", 20),
            Value("DemoAccessProvisioning:Phone", "+1 555 010 0001", 40),
            Value("DemoAccessProvisioning:Email", "ferdi.admin@padelito.example", 120));
    }

    private static string ValidateReceptionPasswordHash(string? passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash) || passwordHash.Length > 500)
        {
            throw new InvalidOperationException(
                "A temporary Reception password hash is required when the configured account does not exist.");
        }

        Span<byte> decoded = stackalloc byte[375];
        if (!Convert.TryFromBase64String(passwordHash, decoded, out var bytesWritten)
            || bytesWritten < 49
            || decoded[0] is not (0 or 1))
        {
            throw new InvalidOperationException("The temporary Reception password hash is invalid.");
        }

        return passwordHash;
    }

    private sealed record ProvisioningValues(
        string AdminUsername,
        string ReceptionUsername,
        int ReceptionPersonId,
        string ReceptionFirstName,
        string ReceptionLastName,
        string? ReceptionPasswordHash,
        string PrivateUsername,
        string PrivatePassword,
        string FirstName,
        string LastName,
        string Dni,
        string Phone,
        string Email);
}
