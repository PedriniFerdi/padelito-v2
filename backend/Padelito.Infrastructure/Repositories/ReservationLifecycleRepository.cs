using System.Data;
using Microsoft.EntityFrameworkCore;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Infrastructure.Repositories;

public sealed class ReservationLifecycleRepository(PadelitoDbContext dbContext) : IReservationLifecycleRepository
{
    public async Task ReconcileAsync(
        int? clubId,
        DateTime localNow,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable, cancellationToken);

        var query = dbContext.Reservations
            .Include(x => x.AvailableTurn).ThenInclude(x => x.Court)
            .Include(x => x.Payments)
            .Where(x => x.ReservationStatusId == ReservationStatusIds.Pending
                || x.ReservationStatusId == ReservationStatusIds.Confirmed
                || (x.ReservationStatusId == ReservationStatusIds.Completed
                    && !x.Payments.Any(payment => payment.Amount == x.FinalPrice)));
        if (clubId.HasValue)
        {
            query = query.Where(x => x.AvailableTurn.Court.ClubId == clubId.Value);
        }

        var today = DateOnly.FromDateTime(localNow);
        var reservations = await query
            .Where(x => x.ReservationDate <= today)
            .ToListAsync(cancellationToken);

        foreach (var reservation in reservations)
        {
            var start = reservation.ReservationDate.ToDateTime(reservation.AvailableTurn.StartTime);
            var end = reservation.ReservationDate.ToDateTime(reservation.AvailableTurn.EndTime);
            var fullyPaid = reservation.Payments.Count == 1
                && reservation.Payments.Single().Amount == reservation.FinalPrice;

            if (localNow >= end && fullyPaid)
            {
                reservation.ReservationStatusId = ReservationStatusIds.Completed;
                AddAudit(reservation, "AutoCompleted",
                    "Reservation completed automatically after the time slot ended.", utcNow);
            }
            else if (localNow >= start && !fullyPaid)
            {
                reservation.ReservationStatusId = ReservationStatusIds.Cancelled;
                AddAudit(reservation, "AutoCanceled",
                    "Reservation canceled automatically because full payment was not received before the time slot started.", utcNow);
            }
            else if (fullyPaid && reservation.ReservationStatusId == ReservationStatusIds.Pending)
            {
                reservation.ReservationStatusId = ReservationStatusIds.Confirmed;
                AddAudit(reservation, "AutoConfirmed",
                    "Reservation confirmed automatically because full payment was received.", utcNow);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static void AddAudit(Reservation reservation, string action, string description, DateTime utcNow)
    {
        reservation.Audits.Add(new ReservationAudit
        {
            ReservationId = reservation.Id,
            Action = action,
            Description = description,
            Username = "system",
            CreatedAt = utcNow
        });
    }
}
