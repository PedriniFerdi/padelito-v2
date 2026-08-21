using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Padelito.Api.Startup;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class DemoAccessProvisionerTests
{
    [Fact]
    public async Task Provisioning_marks_public_users_and_creates_real_private_admin_idempotently()
    {
        await using var provider = CreateProvider();
        await SeedExistingDemoAsync(provider, includeReception: true);
        var configuration = CreateConfiguration();

        await DemoAccessProvisioner.InitializeAsync(provider, configuration, NullLogger.Instance);
        await DemoAccessProvisioner.InitializeAsync(provider, configuration, NullLogger.Instance);

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        var users = await dbContext.Users.Include(x => x.Employee).ThenInclude(x => x.Person).ToListAsync();
        Assert.True(users.Single(x => x.Username == "admin").IsDemo);
        Assert.True(users.Single(x => x.Username == "juanperez").IsDemo);
        var privateAdmin = users.Single(x => x.Username == "Ferdi");
        Assert.False(privateAdmin.IsDemo);
        Assert.Equal(1, privateAdmin.RoleId);
        Assert.Equal("99000001", privateAdmin.Employee.Person.Dni);
        Assert.Equal(3, users.Count);
    }

    [Fact]
    public async Task Provisioning_creates_missing_reception_for_the_exact_employee_and_preserves_inactive_legacy_user()
    {
        await using var provider = CreateProvider();
        await SeedExistingDemoAsync(provider, includeReception: false);
        await AddReceptionEmployeeAndLegacyUserAsync(provider);
        var configuration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["DemoAccessProvisioning:ReceptionPasswordHash"] = CreatePasswordHash("juanperez")
        });

        await DemoAccessProvisioner.InitializeAsync(provider, configuration, NullLogger.Instance);

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        var users = await dbContext.Users.Include(x => x.Employee).ThenInclude(x => x.Person).ToListAsync();
        var reception = users.Single(x => x.Username == "juanperez");
        Assert.True(reception.IsActive);
        Assert.True(reception.IsDemo);
        Assert.Equal(2, reception.RoleId);
        Assert.Equal(3, reception.Employee.PersonId);
        Assert.True(reception.Employee.Person.IsActive);
        Assert.False(users.Single(x => x.Username == "jorge").IsActive);
    }

    [Fact]
    public async Task Provisioning_missing_reception_hash_rolls_back_all_changes()
    {
        await using var provider = CreateProvider();
        await SeedExistingDemoAsync(provider, includeReception: false);
        await AddReceptionEmployeeAndLegacyUserAsync(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            DemoAccessProvisioner.InitializeAsync(provider, CreateConfiguration(), NullLogger.Instance));

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        Assert.False((await dbContext.Users.SingleAsync(x => x.Username == "admin")).IsDemo);
        Assert.DoesNotContain(await dbContext.Users.ToListAsync(), x => x.Username is "Ferdi" or "juanperez");
    }

    [Fact]
    public async Task Disabled_provisioning_does_not_require_a_password_or_change_users()
    {
        await using var provider = CreateProvider();
        await SeedExistingDemoAsync(provider, includeReception: true);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["DemoAccessProvisioning:Enabled"] = "false" }).Build();

        await DemoAccessProvisioner.InitializeAsync(provider, configuration, NullLogger.Instance);

        await using var scope = provider.CreateAsyncScope();
        var users = await scope.ServiceProvider.GetRequiredService<PadelitoDbContext>().Users.ToListAsync();
        Assert.All(users, user => Assert.False(user.IsDemo));
        Assert.Equal(2, users.Count);
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        var root = new InMemoryDatabaseRoot();
        var databaseName = $"demo-provisioning-{Guid.NewGuid()}";
        services.AddDbContext<PadelitoDbContext>(options => options
            .UseInMemoryDatabase(databaseName, root)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }

    private static async Task SeedExistingDemoAsync(IServiceProvider provider, bool includeReception)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
        var club = new Club { Id = 1, Name = "Demo", IsActive = true, CreatedAt = DateTime.UtcNow };
        var adminPerson = CreatePerson(1, "Admin", "10000001");
        var adminEmployee = new Employee { Id = 1, ClubId = 1, PersonId = 1 };
        var admin = CreateUser(1, "admin", 1, 1);
        dbContext.AddRange(club, adminPerson, adminEmployee, admin);

        if (includeReception)
        {
            dbContext.AddRange(
                CreatePerson(2, "Reception", "10000002"),
                new Employee { Id = 2, ClubId = 1, PersonId = 2 },
                CreateUser(2, "juanperez", 2, 2));
        }

        await dbContext.SaveChangesAsync();
    }

    private static Person CreatePerson(int id, string firstName, string dni) => new()
    {
        Id = id,
        FirstName = firstName,
        LastName = "Demo",
        Dni = dni,
        Phone = "+1 555 010 1000",
        Email = $"{firstName.ToLowerInvariant()}@example.test",
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    private static User CreateUser(int id, string username, int employeeId, int roleId, bool isActive = true)
    {
        var user = new User
        {
            Id = id,
            Username = username,
            PasswordHash = string.Empty,
            EmployeeId = employeeId,
            RoleId = roleId,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, "public-demo-password");
        return user;
    }

    private static async Task AddReceptionEmployeeAndLegacyUserAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        var juan = CreatePerson(3, "Juan", "10000003");
        juan.LastName = "Perez";
        dbContext.AddRange(
            juan,
            new Employee { Id = 3, ClubId = 1, PersonId = 3 },
            CreatePerson(4, "Jorge", "10000004"),
            new Employee { Id = 4, ClubId = 1, PersonId = 4 },
            CreateUser(4, "jorge", 4, 2, isActive: false));
        await dbContext.SaveChangesAsync();
    }

    private static string CreatePasswordHash(string username)
    {
        var user = new User { Username = username, PasswordHash = string.Empty };
        return new PasswordHasher<User>().HashPassword(user, "public-demo-password");
    }

    private static IConfiguration CreateConfiguration(
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["DemoAccessProvisioning:Enabled"] = "true",
            ["DemoAccessProvisioning:PrivatePassword"] = "A-private-test-password-2026"
        };
        if (overrides is not null)
        {
            foreach (var (key, value) in overrides)
            {
                values[key] = value;
            }
        }

        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }
}
