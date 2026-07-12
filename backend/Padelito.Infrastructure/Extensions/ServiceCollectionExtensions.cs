using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Infrastructure.Data;
using Padelito.Infrastructure.Repositories;

namespace Padelito.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PadelitoDb")
            ?? throw new InvalidOperationException("Connection string 'PadelitoDb' was not found.");

        services.AddDbContext<PadelitoDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICatalogRepository, CatalogRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();

        return services;
    }
}
