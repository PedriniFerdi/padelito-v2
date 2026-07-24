using Padelito.Application.Common;
using Padelito.Application.DTOs.Reservations;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Services;
using Padelito.Domain.Entities;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class ReservationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 10, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(60, 1, 100)]
    [InlineData(90, 2, 150)]
    [InlineData(120, 1, 200)]
    public async Task Create_calculates_duration_and_accepts_initial_status(int minutes, int statusId, decimal expected)
    {
        var fixture = CreateFixture(minutes);
        var result = await fixture.Service.CreateAsync(1, 1, "admin", Request(statusId: statusId), default);

        Assert.Equal(expected, result.BasePrice);
        Assert.Equal(expected, result.FinalPrice);
        Assert.Equal(statusId, result.ReservationStatusId);
    }

    [Fact]
    public async Task Create_applies_valid_promotion_and_rounds_to_two_decimals()
    {
        var fixture = CreateFixture(90);
        fixture.Repository.Promotions[1] = new Promotion
        {
            Id = 1, Name = "Club", DiscountPercentage = 12.5m,
            DateFrom = new DateOnly(2026, 7, 1), DateTo = new DateOnly(2026, 7, 31), IsActive = true
        };

        var result = await fixture.Service.CreateAsync(1, 1, "admin", Request(promotionId: 1), default);

        Assert.Equal(131.25m, result.FinalPrice);
    }

    [Theory]
    [InlineData(false, "2026-07-01", "2026-07-31")]
    [InlineData(true, "2026-06-01", "2026-06-30")]
    [InlineData(true, "2026-08-01", "2026-08-31")]
    public async Task Create_rejects_invalid_promotion(bool active, string from, string to)
    {
        var fixture = CreateFixture();
        fixture.Repository.Promotions[1] = new Promotion
        {
            Id = 1, Name = "Promo", DiscountPercentage = 10,
            DateFrom = DateOnly.Parse(from), DateTo = DateOnly.Parse(to), IsActive = active
        };

        await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.CreateAsync(1, 1, "admin", Request(promotionId: 1), default));
    }

    [Fact]
    public async Task Create_rejects_inactive_entities_and_turn_from_another_club()
    {
        var inactiveClient = CreateFixture();
        inactiveClient.Repository.Clients[1].Person.IsActive = false;
        await Assert.ThrowsAsync<BusinessException>(() => inactiveClient.Service.CreateAsync(1, 1, "admin", Request(), default));

        var inactiveTurn = CreateFixture();
        inactiveTurn.Repository.Turns[1].IsActive = false;
        await Assert.ThrowsAsync<BusinessException>(() => inactiveTurn.Service.CreateAsync(1, 1, "admin", Request(), default));

        var inactiveCourt = CreateFixture();
        inactiveCourt.Repository.Turns[1].Court.IsActive = false;
        await Assert.ThrowsAsync<BusinessException>(() => inactiveCourt.Service.CreateAsync(1, 1, "admin", Request(), default));

        var otherClub = CreateFixture();
        otherClub.Repository.Turns[1].Court.ClubId = 2;
        await Assert.ThrowsAsync<BusinessException>(() => otherClub.Service.CreateAsync(1, 1, "admin", Request(), default));
    }

    [Theory]
    [InlineData("2026-07-09", 14)]
    [InlineData("2026-07-10", 10)]
    public async Task Create_rejects_past_date_or_time(string date, int startHour)
    {
        var fixture = CreateFixture();
        fixture.Repository.Turns[1].StartTime = new TimeOnly(startHour, 0);
        fixture.Repository.Turns[1].EndTime = new TimeOnly(startHour + 1, 0);

        await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.CreateAsync(1, 1, "admin", Request(DateOnly.Parse(date)), default));
    }

    [Fact]
    public async Task Cancelled_reservation_releases_slot_but_active_reservation_blocks_it()
    {
        var occupied = CreateFixture();
        occupied.Repository.Reservations.Add(ReservationForStatus(ReservationStatusIds.Pending));
        await Assert.ThrowsAsync<ConflictException>(() => occupied.Service.CreateAsync(1, 1, "admin", Request(), default));

        var cancelled = CreateFixture();
        cancelled.Repository.Reservations.Add(ReservationForStatus(ReservationStatusIds.Cancelled));
        var created = await cancelled.Service.CreateAsync(1, 1, "admin", Request(), default);
        Assert.True(created.Id > 0);
    }

    [Fact]
    public async Task Database_collision_is_exposed_as_conflict()
    {
        var fixture = CreateFixture();
        fixture.Repository.ThrowConflictOnSave = true;
        await Assert.ThrowsAsync<ConflictException>(() => fixture.Service.CreateAsync(1, 1, "admin", Request(), default));
    }

    [Fact]
    public async Task Availability_returns_only_future_unoccupied_active_turns()
    {
        var fixture = CreateFixture();
        var pastCourt = fixture.Repository.Turns[1].Court;
        fixture.Repository.Turns[2] = new AvailableTurn
        {
            Id = 2, CourtId = 1, Court = pastCourt,
            StartTime = new TimeOnly(10, 0), EndTime = new TimeOnly(11, 0), IsActive = true
        };
        fixture.Repository.Turns[3] = new AvailableTurn
        {
            Id = 3, CourtId = 1, Court = pastCourt,
            StartTime = new TimeOnly(16, 0), EndTime = new TimeOnly(17, 0), IsActive = false
        };

        var result = await fixture.Service.GetAvailabilityAsync(1, new DateOnly(2026, 7, 10), default);

        var available = Assert.Single(result);
        Assert.Equal(1, available.AvailableTurnId);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(1, 3)]
    [InlineData(2, 3)]
    [InlineData(2, 4)]
    public async Task ChangeStatus_accepts_valid_transitions(int current, int next)
    {
        var fixture = CreateFixture();
        fixture.Repository.Reservations.Add(ReservationForStatus(current));
        var result = await fixture.Service.ChangeStatusAsync(10, 1, "admin", new(next), default);
        Assert.Equal(next, result.ReservationStatusId);
        Assert.Contains(result.Status, fixture.Repository.Statuses.Values.Select(x => x.Name));
    }

    [Theory]
    [InlineData(1, 4)]
    [InlineData(2, 1)]
    [InlineData(3, 1)]
    [InlineData(4, 3)]
    public async Task ChangeStatus_rejects_invalid_transitions(int current, int next)
    {
        var fixture = CreateFixture();
        fixture.Repository.Reservations.Add(ReservationForStatus(current));
        await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.ChangeStatusAsync(10, 1, "admin", new(next), default));
    }

    [Fact]
    public async Task ChangeStatus_blocks_cancellation_when_reservation_has_payments()
    {
        var fixture = CreateFixture();
        var reservation = ReservationForStatus(ReservationStatusIds.Confirmed);
        reservation.Payments.Add(new Payment { Id = 1, ReservationId = reservation.Id, Amount = 50 });
        fixture.Repository.Reservations.Add(reservation);
        await Assert.ThrowsAsync<BusinessException>(() => fixture.Service.ChangeStatusAsync(
            reservation.Id, 1, "admin", new(ReservationStatusIds.Cancelled), default));
    }

    [Fact]
    public async Task Create_and_status_change_write_audit_with_username()
    {
        var fixture = CreateFixture();
        await fixture.Service.CreateAsync(1, 1, "recepcion", Request(), default);
        var reservation = Assert.Single(fixture.Repository.Reservations);
        var creation = Assert.Single(reservation.Audits);
        Assert.Equal("recepcion", creation.Username);
        Assert.Equal("Created", creation.Action);

        await fixture.Service.ChangeStatusAsync(reservation.Id, 1, "recepcion", new(ReservationStatusIds.Confirmed), default);
        Assert.Contains(reservation.Audits, audit => audit.Action == "StatusChanged" && audit.Username == "recepcion");
    }

    [Fact]
    public async Task List_separates_active_and_history()
    {
        var fixture = CreateFixture();
        fixture.Repository.Reservations.AddRange(
            ReservationForStatus(1, 10), ReservationForStatus(2, 11),
            ReservationForStatus(3, 12), ReservationForStatus(4, 13));

        var active = await fixture.Service.GetReservationsAsync(1, new("active"), default);
        var history = await fixture.Service.GetReservationsAsync(1, new("history"), default);

        Assert.Equal(2, active.Count);
        Assert.Equal(2, history.Count);
        Assert.All(active, item => Assert.Contains(item.ReservationStatusId, ReservationStatusIds.Active));
        Assert.All(history, item => Assert.Contains(item.ReservationStatusId, ReservationStatusIds.History));
    }

    [Fact]
    public async Task OperationsBoard_groups_timeline_and_calculates_payment_work()
    {
        var fixture = CreateFixture();
        var pending = ReservationForStatus(ReservationStatusIds.Pending, 10);
        pending.Payments.Add(new Payment { Id = 1, ReservationId = pending.Id, Amount = 40 });
        var confirmed = ReservationForStatus(ReservationStatusIds.Confirmed, 11);
        confirmed.AvailableTurn = new AvailableTurn
        {
            Id = 2,
            CourtId = 2,
            Court = new Court { Id = 2, ClubId = 1, CourtTypeId = 1, Name = "Lateral", HourPrice = 120, IsActive = true, CourtType = new CourtType { Id = 1, Description = "Synthetic Turf" } },
            StartTime = new TimeOnly(12, 30),
            EndTime = new TimeOnly(13, 30),
            IsActive = true
        };
        confirmed.AvailableTurnId = 2;
        confirmed.Payments.Add(new Payment { Id = 2, ReservationId = confirmed.Id, Amount = 100 });
        var cancelled = ReservationForStatus(ReservationStatusIds.Cancelled, 12);
        fixture.Repository.Reservations.AddRange(pending, confirmed, cancelled);

        var board = await fixture.Service.GetOperationsBoardAsync(1, default);

        Assert.Equal(new DateOnly(2026, 7, 10), board.OperationalDate);
        Assert.Equal(2, board.ReservationsToday);
        Assert.Equal(2, board.TimelineByCourt.Count);
        var unpaid = Assert.Single(board.UpcomingUnpaidReservations);
        Assert.Equal(pending.Id, unpaid.Id);
        Assert.Equal(60, unpaid.PendingBalance);
        var soon = Assert.Single(board.StartingSoonReservations);
        Assert.Equal(confirmed.Id, soon.Id);
    }

    private static (ReservationService Service, FakeReservationRepository Repository) CreateFixture(int durationMinutes = 60)
    {
        var repository = new FakeReservationRepository();
        var court = new Court { Id = 1, ClubId = 1, CourtTypeId = 1, Name = "Central", HourPrice = 100, IsActive = true, CourtType = new CourtType { Id = 1, Description = "Synthetic Turf" } };
        repository.Turns[1] = new AvailableTurn { Id = 1, CourtId = 1, Court = court, StartTime = new TimeOnly(14, 0), EndTime = new TimeOnly(14, 0).AddMinutes(durationMinutes), IsActive = true };
        repository.Clients[1] = new Client { Id = 1, PersonId = 2, Person = Person(2, "Ana", "Paz") };
        repository.Employees[1] = new Employee { Id = 1, PersonId = 1, ClubId = 1, Person = Person(1, "Carlos", "Benitez") };
        repository.Statuses[1] = new ReservationStatus { Id = 1, Name = "Pending" };
        repository.Statuses[2] = new ReservationStatus { Id = 2, Name = "Confirmed" };
        repository.Statuses[3] = new ReservationStatus { Id = 3, Name = "Canceled" };
        repository.Statuses[4] = new ReservationStatus { Id = 4, Name = "Completed" };
        return (new ReservationService(repository, new FixedTimeProvider(Now), TimeZoneInfo.Utc), repository);
    }

    private static ReservationCreateDto Request(DateOnly? date = null, int statusId = 1, int? promotionId = null) =>
        new(1, 1, promotionId, date ?? new DateOnly(2026, 7, 10), statusId);

    private static Person Person(int id, string firstName, string lastName) =>
        new() { Id = id, FirstName = firstName, LastName = lastName, Dni = $"30{id:000000}", Phone = "1140001001", Email = $"persona{id}@example.com", IsActive = true, CreatedAt = Now.UtcDateTime };

    private static Reservation ReservationForStatus(int statusId, int id = 10)
    {
        var fixture = CreateFixture();
        var status = fixture.Repository.Statuses[statusId];
        return new Reservation
        {
            Id = id, ClientId = 1, Client = fixture.Repository.Clients[1], AvailableTurnId = 1,
            AvailableTurn = fixture.Repository.Turns[1], EmployeeId = 1, Employee = fixture.Repository.Employees[1],
            ReservationDate = new DateOnly(2026, 7, 10), ReservationStatusId = statusId, ReservationStatus = status,
            BasePrice = 100, FinalPrice = 100, CreatedAt = Now.UtcDateTime
        };
    }
}

internal sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}

internal sealed class FakeReservationRepository : IReservationRepository
{
    public Dictionary<int, Client> Clients { get; } = [];
    public Dictionary<int, Employee> Employees { get; } = [];
    public Dictionary<int, AvailableTurn> Turns { get; } = [];
    public Dictionary<int, Promotion> Promotions { get; } = [];
    public Dictionary<int, ReservationStatus> Statuses { get; } = [];
    public List<Reservation> Reservations { get; } = [];
    public bool ThrowConflictOnSave { get; set; }

    public Task<List<Reservation>> GetReservationsAsync(int clubId, IReadOnlyCollection<int> statusIds, DateOnly? dateFrom, DateOnly? dateTo, int? statusId, CancellationToken cancellationToken) =>
        Task.FromResult(Reservations.Where(x => x.AvailableTurn.Court.ClubId == clubId && statusIds.Contains(x.ReservationStatusId)
            && (!dateFrom.HasValue || x.ReservationDate >= dateFrom) && (!dateTo.HasValue || x.ReservationDate <= dateTo)
            && (!statusId.HasValue || x.ReservationStatusId == statusId)).ToList());

    public Task<Reservation?> GetReservationAsync(int id, int clubId, bool trackChanges, CancellationToken cancellationToken) =>
        Task.FromResult(Reservations.FirstOrDefault(x => x.Id == id && x.AvailableTurn.Court.ClubId == clubId));

    public Task<List<Reservation>> GetOperationsBoardReservationsAsync(int clubId, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(Reservations.Where(x => x.AvailableTurn.Court.ClubId == clubId && x.ReservationDate == date)
            .OrderBy(x => x.AvailableTurn.Court.Name).ThenBy(x => x.AvailableTurn.StartTime).ThenBy(x => x.Id).ToList());

    public Task<List<AvailableTurn>> GetAvailabilityAsync(int clubId, DateOnly date, CancellationToken cancellationToken) =>
        Task.FromResult(Turns.Values.Where(x => x.IsActive && x.Court.IsActive && x.Court.ClubId == clubId && !Reservations.Any(r => r.AvailableTurnId == x.Id && r.ReservationDate == date && r.ReservationStatusId != 3)).ToList());

    public Task<Client?> GetClientAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Clients.GetValueOrDefault(id));
    public Task<Employee?> GetEmployeeAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Employees.GetValueOrDefault(id));
    public Task<AvailableTurn?> GetAvailableTurnAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Turns.GetValueOrDefault(id));
    public Task<Promotion?> GetPromotionAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Promotions.GetValueOrDefault(id));
    public Task<ReservationStatus?> GetStatusAsync(int id, CancellationToken cancellationToken) => Task.FromResult(Statuses.GetValueOrDefault(id));
    public Task<bool> IsOccupiedAsync(DateOnly date, int availableTurnId, CancellationToken cancellationToken) =>
        Task.FromResult(Reservations.Any(x => x.ReservationDate == date && x.AvailableTurnId == availableTurnId && x.ReservationStatusId != 3));

    public Task<bool> HasPaymentsAsync(int reservationId, CancellationToken cancellationToken) =>
        Task.FromResult(Reservations.FirstOrDefault(x => x.Id == reservationId)?.Payments.Count > 0);

    public Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        reservation.Id = Reservations.Count == 0 ? 1 : Reservations.Max(x => x.Id) + 1;
        Reservations.Add(reservation);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        if (ThrowConflictOnSave) throw new ConflictException("Conflicto simulado.");
        return Task.CompletedTask;
    }
}
