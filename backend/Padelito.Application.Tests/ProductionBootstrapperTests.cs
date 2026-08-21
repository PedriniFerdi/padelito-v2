using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Padelito.Api.Startup;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;
using Padelito.Infrastructure.Data.Migrations;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class ProductionBootstrapperTests
{
    [Fact]
    public async Task Bootstrap_creates_one_admin_and_is_idempotent()
    {
        await using var provider = CreateProvider();
        await EnsureDatabaseCreatedAsync(provider);
        var configuration = CreateConfiguration();

        await ProductionBootstrapper.InitializeAsync(provider, configuration, NullLogger.Instance);
        await ProductionBootstrapper.InitializeAsync(provider, configuration, NullLogger.Instance);

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        var user = await dbContext.Users.Include(x => x.Employee).ThenInclude(x => x.Club).SingleAsync();
        Assert.Equal("admin.production", user.Username);
        Assert.Equal("Padelito Production", user.Employee.Club.Name);
        Assert.Single(await dbContext.Clubs.ToListAsync());
        Assert.Single(await dbContext.People.ToListAsync());
        Assert.Single(await dbContext.Employees.ToListAsync());
        Assert.Empty(await dbContext.Clients.ToListAsync());
        Assert.Empty(await dbContext.Reservations.ToListAsync());
        Assert.Empty(await dbContext.Payments.ToListAsync());
    }

    [Fact]
    public async Task Disabled_bootstrap_does_not_require_configuration_or_write_data()
    {
        await using var provider = CreateProvider();
        await EnsureDatabaseCreatedAsync(provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Bootstrap:Enabled"] = "false" }).Build();

        await ProductionBootstrapper.InitializeAsync(provider, configuration, NullLogger.Instance);

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        Assert.Empty(await dbContext.Clubs.ToListAsync());
        Assert.Empty(await dbContext.Users.ToListAsync());
    }

    [Fact]
    public async Task Bootstrap_rejects_incomplete_or_weak_credentials()
    {
        await using var provider = CreateProvider();
        await EnsureDatabaseCreatedAsync(provider);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Bootstrap:Enabled"] = "true",
            ["Bootstrap:AdminPassword"] = "short"
        }).Build();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProductionBootstrapper.InitializeAsync(provider, configuration, NullLogger.Instance));
    }

    [Fact]
    public async Task Bootstrap_rejects_partial_state_without_changing_it()
    {
        await using var provider = CreateProvider();
        await EnsureDatabaseCreatedAsync(provider);
        await using (var seedScope = provider.CreateAsyncScope())
        {
            var dbContext = seedScope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
            dbContext.Clubs.Add(new Club
            {
                Name = "Existing club",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            });
            await dbContext.SaveChangesAsync();
        }

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProductionBootstrapper.InitializeAsync(provider, CreateConfiguration(), NullLogger.Instance));

        Assert.Contains("empty application database", exception.Message, StringComparison.Ordinal);
        await using var assertionScope = provider.CreateAsyncScope();
        var assertionContext = assertionScope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        Assert.Single(await assertionContext.Clubs.ToListAsync());
        Assert.Empty(await assertionContext.People.ToListAsync());
        Assert.Empty(await assertionContext.Users.ToListAsync());
    }

    [Fact]
    public async Task Bootstrap_rejects_completed_graph_that_does_not_match_configuration()
    {
        await using var provider = CreateProvider();
        await EnsureDatabaseCreatedAsync(provider);
        await ProductionBootstrapper.InitializeAsync(provider, CreateConfiguration(), NullLogger.Instance);
        var changedConfiguration = CreateConfiguration(new Dictionary<string, string?>
        {
            ["Bootstrap:ClubName"] = "Another club"
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            ProductionBootstrapper.InitializeAsync(provider, changedConfiguration, NullLogger.Instance));

        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        Assert.Equal("Padelito Production", (await dbContext.Clubs.SingleAsync()).Name);
        Assert.Single(await dbContext.Users.ToListAsync());
    }

    [Fact]
    public void Production_migrations_do_not_mutate_application_owned_data()
    {
        var migrations = new Migration[]
        {
            new InitialCreate(),
            new SetAdminDemoPassword(),
            new CompleteDemoSeed(),
            new PrepareProductionData(),
            new AddUsPortfolioDemoSeed(),
            new AddDemoUserFlag()
        };
        var allowedSeedTables = new HashSet<string>(StringComparer.Ordinal)
        {
            "CourtTypes",
            "PaymentMethods",
            "ReservationStatuses",
            "Roles"
        };

        foreach (var migration in migrations)
        {
            var operations = BuildUpOperations(migration);
            Assert.DoesNotContain(operations, operation => operation is DeleteDataOperation or UpdateDataOperation);
            Assert.DoesNotContain(operations, operation => operation is SqlOperation);
            Assert.All(
                operations.OfType<InsertDataOperation>(),
                operation => Assert.Contains(operation.Table, allowedSeedTables));
        }
    }

    [Fact]
    public async Task Model_seed_is_limited_to_global_reference_catalogs()
    {
        await using var provider = CreateProvider();
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();

        var designTimeModel = dbContext.GetService<IDesignTimeModel>().Model;
        var seededTypes = designTimeModel.GetEntityTypes()
            .Where(entityType => entityType.GetSeedData().Any())
            .Select(entityType => entityType.ClrType)
            .ToHashSet();

        var expectedTypes = new HashSet<Type>
        {
            typeof(Role),
            typeof(ReservationStatus),
            typeof(PaymentMethod),
            typeof(CourtType)
        };
        Assert.True(expectedTypes.SetEquals(seededTypes));
    }

    private static IReadOnlyList<MigrationOperation> BuildUpOperations(Migration migration)
    {
        var builder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");
        var method = migration.GetType().GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Migration {migration.GetType().Name} has no Up method.");
        method.Invoke(migration, [builder]);
        return builder.Operations;
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = $"bootstrap-{Guid.NewGuid()}";
        services.AddDbContext<PadelitoDbContext>(options => options
            .UseInMemoryDatabase(databaseName, databaseRoot)
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));
        return services.BuildServiceProvider();
    }

    private static async Task EnsureDatabaseCreatedAsync(IServiceProvider provider)
    {
        await using var scope = provider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PadelitoDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    private static IConfiguration CreateConfiguration(Dictionary<string, string?>? overrides = null)
    {
        var values = new Dictionary<string, string?>
        {
            ["Bootstrap:Enabled"] = "true",
            ["Bootstrap:ClubName"] = "Padelito Production",
            ["Bootstrap:AdminUsername"] = "admin.production",
            ["Bootstrap:AdminPassword"] = "A-strong-password-2026",
            ["Bootstrap:AdminFirstName"] = "Admin",
            ["Bootstrap:AdminLastName"] = "Padelito",
            ["Bootstrap:AdminDni"] = "30.111.222",
            ["Bootstrap:AdminPhone"] = "+54 11 4000 1001",
            ["Bootstrap:AdminEmail"] = "ADMIN@PADELITO.TEST"
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
