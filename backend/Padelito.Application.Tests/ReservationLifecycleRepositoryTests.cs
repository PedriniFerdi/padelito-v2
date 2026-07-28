using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;
using Padelito.Infrastructure.Repositories;
using Xunit;

namespace Padelito.Application.Tests;

public sealed class ReservationLifecycleRepositoryTests
{
    [Fact]
    public async Task Reconcile_cancels_unpaid_reservation_at_start_and_is_idempotent()
    {
        await using var dbContext = CreateContext();
        var reservation = ReservationAt(ReservationStatusIds.Pending, new TimeOnly(10, 0), new TimeOnly(11, 0));
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync();
        var repository = new ReservationLifecycleRepository(dbContext);
        var now = new DateTime(2026, 7, 24, 10, 0, 0);

        await repository.ReconcileAsync(1, now, DateTime.SpecifyKind(now, DateTimeKind.Utc), default);
        await repository.ReconcileAsync(1, now.AddMinutes(1), DateTime.SpecifyKind(now.AddMinutes(1), DateTimeKind.Utc), default);

        Assert.Equal(ReservationStatusIds.Cancelled, reservation.ReservationStatusId);
        var audit = Assert.Single(reservation.Audits);
        Assert.Equal("AutoCanceled", audit.Action);
        Assert.Equal("system", audit.Username);
    }

    [Fact]
    public async Task Reconcile_completes_fully_paid_reservation_at_end()
    {
        await using var dbContext = CreateContext();
        var reservation = ReservationAt(ReservationStatusIds.Confirmed, new TimeOnly(10, 0), new TimeOnly(11, 0));
        reservation.Payments.Add(new Payment
        {
            Reservation = reservation,
            ReservationId = reservation.Id,
            PaymentMethodId = 1,
            Amount = reservation.FinalPrice,
            PaymentDate = new DateTime(2026, 7, 24, 9, 0, 0)
        });
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync();
        var repository = new ReservationLifecycleRepository(dbContext);
        var now = new DateTime(2026, 7, 24, 11, 0, 0);

        await repository.ReconcileAsync(1, now, DateTime.SpecifyKind(now, DateTimeKind.Utc), default);

        Assert.Equal(ReservationStatusIds.Completed, reservation.ReservationStatusId);
        var audit = Assert.Single(reservation.Audits);
        Assert.Equal("AutoCompleted", audit.Action);
        Assert.Equal("system", audit.Username);
    }

    [Fact]
    public async Task Reconcile_repairs_historical_completed_reservation_without_full_payment()
    {
        await using var dbContext = CreateContext();
        var reservation = ReservationAt(ReservationStatusIds.Completed, new TimeOnly(10, 0), new TimeOnly(11, 0));
        dbContext.Reservations.Add(reservation);
        await dbContext.SaveChangesAsync();
        var repository = new ReservationLifecycleRepository(dbContext);
        var now = new DateTime(2026, 7, 24, 12, 0, 0);

        await repository.ReconcileAsync(1, now, DateTime.SpecifyKind(now, DateTimeKind.Utc), default);

        Assert.Equal(ReservationStatusIds.Cancelled, reservation.ReservationStatusId);
        var audit = Assert.Single(reservation.Audits);
        Assert.Equal("AutoCanceled", audit.Action);
        Assert.Equal("system", audit.Username);
    }

    private static PadelitoDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PadelitoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new PadelitoDbContext(options);
    }

    private static Reservation ReservationAt(int statusId, TimeOnly start, TimeOnly end)
    {
        var court = new Court { Id = 501, ClubId = 1, CourtTypeId = 1, Name = "Center Court", HourPrice = 25, IsActive = true };
        return new Reservation
        {
            Id = 501,
            ClientId = 1,
            AvailableTurnId = 501,
            AvailableTurn = new AvailableTurn
            {
                Id = 501,
                CourtId = court.Id,
                Court = court,
                StartTime = start,
                EndTime = end,
                IsActive = true
            },
            EmployeeId = 1,
            ReservationDate = new DateOnly(2026, 7, 24),
            ReservationStatusId = statusId,
            BasePrice = 25,
            FinalPrice = 25,
            CreatedAt = new DateTime(2026, 7, 23)
        };
    }
}
