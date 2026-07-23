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
        Assert.Equal("Padelito Producción", user.Employee.Club.Name);
        Assert.Single(await dbContext.People.ToListAsync());
        Assert.Single(await dbContext.Employees.ToListAsync());
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
        if (!await dbContext.Roles.AnyAsync())
        {
            dbContext.Roles.Add(new Role { Id = 1, Name = "Administrador" });
            await dbContext.SaveChangesAsync();
        }
    }

    private static IConfiguration CreateConfiguration()
    {
        return new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Bootstrap:Enabled"] = "true",
            ["Bootstrap:ClubName"] = "Padelito Producción",
            ["Bootstrap:AdminUsername"] = "admin.production",
            ["Bootstrap:AdminPassword"] = "A-strong-password-2026",
            ["Bootstrap:AdminFirstName"] = "Admin",
            ["Bootstrap:AdminLastName"] = "Padelito",
            ["Bootstrap:AdminDni"] = "30.111.222",
            ["Bootstrap:AdminPhone"] = "+54 11 4000 1001",
            ["Bootstrap:AdminEmail"] = "ADMIN@PADELITO.TEST"
        }).Build();
    }
}
