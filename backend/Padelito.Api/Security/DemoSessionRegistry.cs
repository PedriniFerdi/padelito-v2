using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Api.Security;

public sealed class DemoSessionRegistry
{
    private readonly ConcurrentDictionary<DemoSessionKey, DemoSession> sessions = new();
    private readonly SemaphoreSlim registryLock = new(1, 1);
    private readonly IProductionPadelitoDbContextFactory productionFactory;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<DemoSessionRegistry> logger;
    private readonly TimeSpan idleTimeout;
    private readonly int maxSessions;

    public DemoSessionRegistry(
        IProductionPadelitoDbContextFactory productionFactory,
        IConfiguration configuration,
        TimeProvider timeProvider,
        ILogger<DemoSessionRegistry> logger)
    {
        this.productionFactory = productionFactory;
        this.timeProvider = timeProvider;
        this.logger = logger;
        var idleMinutes = configuration.GetValue("DemoMode:SessionIdleMinutes", 30);
        maxSessions = configuration.GetValue("DemoMode:MaxSessions", 100);
        if (idleMinutes is < 1 or > 1440 || maxSessions is < 1 or > 1000)
        {
            throw new InvalidOperationException("DemoMode session limits are invalid.");
        }

        idleTimeout = TimeSpan.FromMinutes(idleMinutes);
    }

    public async Task<DemoSession> GetOrCreateAsync(
        int userId,
        int clubId,
        Guid sessionId,
        CancellationToken cancellationToken)
    {
        var key = new DemoSessionKey(userId, sessionId);
        if (sessions.TryGetValue(key, out var current))
        {
            current.Touch(timeProvider.GetUtcNow());
            return current;
        }

        await registryLock.WaitAsync(cancellationToken);
        try
        {
            if (sessions.TryGetValue(key, out current))
            {
                current.Touch(timeProvider.GetUtcNow());
                return current;
            }

            RemoveExpiredCore();
            if (sessions.Count >= maxSessions)
            {
                throw new DemoSessionCapacityException();
            }

            var root = new InMemoryDatabaseRoot();
            var options = new DbContextOptionsBuilder<PadelitoDbContext>()
                .UseInMemoryDatabase($"demo-{userId}-{sessionId:N}", root)
                .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
                .Options;
            await CloneClubAsync(options, clubId, cancellationToken);
            var created = new DemoSession(options, timeProvider.GetUtcNow());
            sessions[key] = created;
            logger.LogInformation("Created isolated demo session {DemoSessionId} for user {UserId}.", sessionId, userId);
            return created;
        }
        finally
        {
            registryLock.Release();
        }
    }

    public async Task RemoveExpiredAsync(CancellationToken cancellationToken)
    {
        await registryLock.WaitAsync(cancellationToken);
        try
        {
            RemoveExpiredCore();
        }
        finally
        {
            registryLock.Release();
        }
    }

    private void RemoveExpiredCore()
    {
        var cutoff = timeProvider.GetUtcNow() - idleTimeout;
        foreach (var (key, session) in sessions)
        {
            if (session.LastAccessUtc < cutoff && sessions.TryRemove(key, out _))
            {
                logger.LogInformation("Expired isolated demo session {DemoSessionId}.", key.SessionId);
            }
        }
    }

    private async Task CloneClubAsync(
        DbContextOptions<PadelitoDbContext> targetOptions,
        int clubId,
        CancellationToken cancellationToken)
    {
        await using var source = productionFactory.CreateDbContext();
        var club = await source.Clubs.AsNoTracking().SingleOrDefaultAsync(x => x.Id == clubId, cancellationToken)
            ?? throw new InvalidOperationException("The authenticated demo club does not exist.");
        var clients = await source.Clients.AsNoTracking().ToListAsync(cancellationToken);
        var employees = await source.Employees.AsNoTracking()
            .Where(x => x.ClubId == clubId)
            .ToListAsync(cancellationToken);
        var personIds = clients.Select(x => x.PersonId)
            .Concat(employees.Select(x => x.PersonId))
            .Distinct()
            .ToArray();
        var people = await source.People.AsNoTracking()
            .Where(x => personIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        var employeeIds = employees.Select(x => x.Id).ToArray();
        var users = await source.Users.AsNoTracking()
            .Where(x => x.IsDemo && employeeIds.Contains(x.EmployeeId))
            .ToListAsync(cancellationToken);
        var courts = await source.Courts.AsNoTracking()
            .Where(x => x.ClubId == clubId)
            .ToListAsync(cancellationToken);
        var courtIds = courts.Select(x => x.Id).ToArray();
        var turns = await source.AvailableTurns.AsNoTracking()
            .Where(x => courtIds.Contains(x.CourtId))
            .ToListAsync(cancellationToken);
        var turnIds = turns.Select(x => x.Id).ToArray();
        var promotions = await source.Promotions.AsNoTracking().ToListAsync(cancellationToken);
        var reservations = await source.Reservations.AsNoTracking()
            .Where(x => turnIds.Contains(x.AvailableTurnId))
            .ToListAsync(cancellationToken);
        var reservationIds = reservations.Select(x => x.Id).ToArray();
        var payments = await source.Payments.AsNoTracking()
            .Where(x => reservationIds.Contains(x.ReservationId))
            .ToListAsync(cancellationToken);
        var audits = await source.ReservationAudits.AsNoTracking()
            .Where(x => reservationIds.Contains(x.ReservationId))
            .ToListAsync(cancellationToken);

        await using var target = new PadelitoDbContext(targetOptions);
        await target.Database.EnsureCreatedAsync(cancellationToken);
        target.Clubs.Add(Copy(club));
        target.People.AddRange(people.Select(Copy));
        target.Clients.AddRange(clients.Select(Copy));
        target.Employees.AddRange(employees.Select(Copy));
        target.Users.AddRange(users.Select(Copy));
        target.Courts.AddRange(courts.Select(Copy));
        target.AvailableTurns.AddRange(turns.Select(Copy));
        target.Promotions.AddRange(promotions.Select(Copy));
        target.Reservations.AddRange(reservations.Select(Copy));
        target.Payments.AddRange(payments.Select(Copy));
        target.ReservationAudits.AddRange(audits.Select(Copy));
        await target.SaveChangesAsync(cancellationToken);
    }

    private static Club Copy(Club x) => new() { Id = x.Id, Name = x.Name, Address = x.Address, Phone = x.Phone, Email = x.Email, IsActive = x.IsActive, CreatedAt = x.CreatedAt };
    private static Person Copy(Person x) => new() { Id = x.Id, FirstName = x.FirstName, LastName = x.LastName, Dni = x.Dni, Phone = x.Phone, Email = x.Email, IsActive = x.IsActive, CreatedAt = x.CreatedAt };
    private static Client Copy(Client x) => new() { Id = x.Id, PersonId = x.PersonId };
    private static Employee Copy(Employee x) => new() { Id = x.Id, PersonId = x.PersonId, ClubId = x.ClubId };
    private static User Copy(User x) => new() { Id = x.Id, Username = x.Username, PasswordHash = x.PasswordHash, EmployeeId = x.EmployeeId, RoleId = x.RoleId, IsDemo = true, IsActive = x.IsActive, CreatedAt = x.CreatedAt };
    private static Court Copy(Court x) => new() { Id = x.Id, ClubId = x.ClubId, CourtTypeId = x.CourtTypeId, Name = x.Name, HourPrice = x.HourPrice, IsActive = x.IsActive };
    private static AvailableTurn Copy(AvailableTurn x) => new() { Id = x.Id, CourtId = x.CourtId, StartTime = x.StartTime, EndTime = x.EndTime, IsActive = x.IsActive };
    private static Promotion Copy(Promotion x) => new() { Id = x.Id, Name = x.Name, Description = x.Description, DiscountPercentage = x.DiscountPercentage, DateFrom = x.DateFrom, DateTo = x.DateTo, IsActive = x.IsActive };
    private static Reservation Copy(Reservation x) => new() { Id = x.Id, ClientId = x.ClientId, AvailableTurnId = x.AvailableTurnId, EmployeeId = x.EmployeeId, PromotionId = x.PromotionId, ReservationDate = x.ReservationDate, ReservationStatusId = x.ReservationStatusId, BasePrice = x.BasePrice, FinalPrice = x.FinalPrice, CreatedAt = x.CreatedAt };
    private static Payment Copy(Payment x) => new() { Id = x.Id, ReservationId = x.ReservationId, PaymentMethodId = x.PaymentMethodId, Amount = x.Amount, PaymentDate = x.PaymentDate, Note = x.Note };
    private static ReservationAudit Copy(ReservationAudit x) => new() { Id = x.Id, ReservationId = x.ReservationId, Action = x.Action, Description = x.Description, Username = x.Username, CreatedAt = x.CreatedAt };

    private readonly record struct DemoSessionKey(int UserId, Guid SessionId);
}

public sealed class DemoSession(
    DbContextOptions<PadelitoDbContext> options,
    DateTimeOffset lastAccessUtc)
{
    public DbContextOptions<PadelitoDbContext> Options { get; } = options;
    public SemaphoreSlim WriteLock { get; } = new(1, 1);
    public DateTimeOffset LastAccessUtc { get; private set; } = lastAccessUtc;

    public void Touch(DateTimeOffset now) => LastAccessUtc = now;
}

public sealed class DemoSessionCapacityException : Exception;

public sealed class DemoSessionCleanupService(
    DemoSessionRegistry registry,
    ILogger<DemoSessionCleanupService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await registry.RemoveExpiredAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Demo session cleanup failed.");
            }
        }
    }
}
