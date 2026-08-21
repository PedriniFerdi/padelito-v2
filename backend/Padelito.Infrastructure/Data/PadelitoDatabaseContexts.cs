using Microsoft.EntityFrameworkCore;

namespace Padelito.Infrastructure.Data;

public sealed class PadelitoDatabaseSessionAccessor
{
    public DbContextOptions<PadelitoDbContext>? SessionOptions { get; set; }
}

public interface IProductionPadelitoDbContextFactory
{
    PadelitoDbContext CreateDbContext();
}

internal sealed class ProductionPadelitoDbContextFactory(
    DbContextOptions<PadelitoDbContext> options) : IProductionPadelitoDbContextFactory
{
    public PadelitoDbContext CreateDbContext() => new(options);
}
