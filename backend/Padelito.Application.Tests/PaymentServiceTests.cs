using Padelito.Application.Common;
using Padelito.Application.DTOs.Payments;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;
using Padelito.Application.Services;
using Padelito.Domain.Entities;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class PaymentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 12, 12, 0, 0, TimeSpan.Zero);
    private static readonly TimeZoneInfo ClubTimeZone = TimeZoneInfo.Utc;

    [Fact]
    public async Task Create_records_the_server_total_and_confirms_pending_reservation()
    {
        var repository = new FakePaymentRepository();
        var service = CreateService(repository);

        var payment = await service.CreateAsync(1, "admin", new(1, 1, "Front desk"), default);

        Assert.Equal(100m, payment.Amount);
        Assert.Equal(0m, payment.PendingBalance);
        Assert.Equal(ReservationStatusIds.Confirmed, repository.Reservation.ReservationStatusId);
        Assert.Single(repository.Reservation.Payments);
    }

    [Fact]
    public async Task Create_rejects_a_second_payment_without_duplicating_records()
    {
        var repository = new FakePaymentRepository();
        var service = CreateService(repository);
        await service.CreateAsync(1, "admin", new(1, 1, null), default);

        await Assert.ThrowsAsync<ConflictException>(
            () => service.CreateAsync(1, "admin", new(1, 1, null), default));

        Assert.Single(repository.Reservation.Payments);
    }

    [Fact]
    public async Task Create_rejects_payment_at_or_after_slot_start()
    {
        var repository = new FakePaymentRepository();
        var service = new PaymentService(
            repository,
            new NoOpReservationLifecycleService(),
            new FixedTimeProvider(new(2026, 7, 12, 14, 0, 0, TimeSpan.Zero)),
            ClubTimeZone);

        await Assert.ThrowsAsync<BusinessException>(
            () => service.CreateAsync(1, "admin", new(1, 1, null), default));
        Assert.Empty(repository.Reservation.Payments);
    }

    [Fact]
    public async Task List_rejects_inverted_date_range()
    {
        var service = CreateService(new FakePaymentRepository());
        await Assert.ThrowsAsync<BusinessException>(() => service.GetPaymentsAsync(1,
            new(new DateOnly(2026, 7, 12), new DateOnly(2026, 7, 11)), default));
    }

    [Fact]
    public async Task List_converts_club_calendar_dates_to_utc_boundaries()
    {
        var repository = new FakePaymentRepository();
        var zone = TimeZoneInfo.CreateCustomTimeZone("Club test", TimeSpan.FromHours(-3), "Club test", "Club test");
        var service = new PaymentService(repository, new NoOpReservationLifecycleService(), new FixedTimeProvider(Now), zone);

        await service.GetPaymentsAsync(1,
            new(new DateOnly(2026, 7, 12), new DateOnly(2026, 7, 12)), default);

        Assert.Equal(new DateTime(2026, 7, 12, 3, 0, 0, DateTimeKind.Utc), repository.LastDateFromUtc);
        Assert.Equal(new DateTime(2026, 7, 13, 3, 0, 0, DateTimeKind.Utc), repository.LastDateToExclusiveUtc);
    }

    private static PaymentService CreateService(FakePaymentRepository repository) =>
        new(repository, new NoOpReservationLifecycleService(), new FixedTimeProvider(Now), ClubTimeZone);
}

internal sealed class NoOpReservationLifecycleService : IReservationLifecycleService
{
    public Task ReconcileAsync(int? clubId, CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class FakePaymentRepository : IPaymentRepository
{
    public DateTime? LastDateFromUtc { get; private set; }
    public DateTime? LastDateToExclusiveUtc { get; private set; }
    public Reservation Reservation { get; }

    public FakePaymentRepository()
    {
        var person = new Person { Id = 1, FirstName = "Ana", LastName = "Paz", Dni = "30111222", Phone = "1140001001", Email = "ana@example.com", IsActive = true };
        var client = new Client { Id = 1, PersonId = 1, Person = person };
        var court = new Court { Id = 1, ClubId = 1, CourtTypeId = 1, Name = "Center Court", IsActive = true };
        Reservation = new Reservation
        {
            Id = 1,
            ClientId = 1,
            Client = client,
            AvailableTurnId = 1,
            AvailableTurn = new AvailableTurn
            {
                Id = 1,
                CourtId = 1,
                Court = court,
                StartTime = new TimeOnly(14, 0),
                EndTime = new TimeOnly(15, 0)
            },
            EmployeeId = 1,
            ReservationDate = new DateOnly(2026, 7, 12),
            ReservationStatusId = ReservationStatusIds.Pending,
            FinalPrice = 100
        };
    }

    public Task<List<PaymentReadModel>> GetPaymentsAsync(int clubId, DateTime? dateFromUtc, DateTime? dateToExclusiveUtc, int? methodId, int? reservationId, CancellationToken cancellationToken)
    {
        LastDateFromUtc = dateFromUtc;
        LastDateToExclusiveUtc = dateToExclusiveUtc;
        return Task.FromResult(new List<PaymentReadModel>());
    }

    public Task<Payment> AddFullPaymentAsync(
        int clubId,
        int reservationId,
        int paymentMethodId,
        string? note,
        string username,
        DateTime localNow,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (localNow >= Reservation.ReservationDate.ToDateTime(Reservation.AvailableTurn.StartTime))
            throw new BusinessException("Payments cannot be recorded after the time slot has started.");
        if (Reservation.Payments.Count != 0)
            throw new ConflictException("This reservation already has a recorded payment.");

        var method = new PaymentMethod { Id = paymentMethodId, Description = "Cash" };
        var payment = new Payment
        {
            Id = 1,
            ReservationId = Reservation.Id,
            Reservation = Reservation,
            PaymentMethodId = method.Id,
            PaymentMethod = method,
            Amount = Reservation.FinalPrice,
            PaymentDate = utcNow,
            Note = note
        };
        Reservation.Payments.Add(payment);
        Reservation.ReservationStatusId = ReservationStatusIds.Confirmed;
        return Task.FromResult(payment);
    }
}
