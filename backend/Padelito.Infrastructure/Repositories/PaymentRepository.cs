using System.Data;
using Microsoft.EntityFrameworkCore;
using Padelito.Application.Common;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Infrastructure.Repositories;

public sealed class PaymentRepository(PadelitoDbContext dbContext) : IPaymentRepository
{
    public Task<List<PaymentReadModel>> GetPaymentsAsync(int clubId, DateTime? dateFromUtc, DateTime? dateToExclusiveUtc, int? methodId, int? reservationId, CancellationToken cancellationToken)
    {
        var query = dbContext.Payments
            .Where(x => x.Reservation.AvailableTurn.Court.ClubId == clubId);
        if (dateFromUtc.HasValue) query = query.Where(x => x.PaymentDate >= dateFromUtc.Value);
        if (dateToExclusiveUtc.HasValue) query = query.Where(x => x.PaymentDate < dateToExclusiveUtc.Value);
        if (methodId.HasValue) query = query.Where(x => x.PaymentMethodId == methodId.Value);
        if (reservationId.HasValue) query = query.Where(x => x.ReservationId == reservationId.Value);
        return query.AsNoTracking()
            .OrderByDescending(x => x.PaymentDate)
            .ThenByDescending(x => x.Id)
            .Select(x => new PaymentReadModel(
                x.Id,
                x.ReservationId,
                x.Reservation.ReservationDate,
                x.Reservation.Client.Person.FirstName + " " + x.Reservation.Client.Person.LastName,
                x.Reservation.AvailableTurn.Court.Name,
                x.PaymentMethodId,
                x.PaymentMethod.Description,
                x.Amount,
                x.PaymentDate,
                x.Note,
                x.Reservation.FinalPrice,
                x.Reservation.Payments.Sum(payment => payment.Amount)))
            .ToListAsync(cancellationToken);
    }

    public Task<Reservation?> GetReservationAsync(int id, int clubId, CancellationToken cancellationToken) =>
        dbContext.Reservations.Include(x => x.Payments).Include(x => x.Client).ThenInclude(x => x.Person)
            .Include(x => x.AvailableTurn).ThenInclude(x => x.Court)
            .FirstOrDefaultAsync(x => x.Id == id && x.AvailableTurn.Court.ClubId == clubId, cancellationToken);

    public Task<PaymentMethod?> GetMethodAsync(int id, CancellationToken cancellationToken) =>
        dbContext.PaymentMethods.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<Payment> AddPaymentAsync(int clubId, Payment payment, CancellationToken cancellationToken)
    {
        // The service performs an early validation for a friendly error. Clear those tracked
        // entities so the transactional query below always observes the latest committed balance.
        dbContext.ChangeTracker.Clear();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var reservation = await dbContext.Reservations.Include(x => x.Payments).Include(x => x.Client).ThenInclude(x => x.Person)
            .Include(x => x.AvailableTurn).ThenInclude(x => x.Court)
            .FirstOrDefaultAsync(x => x.Id == payment.ReservationId && x.AvailableTurn.Court.ClubId == clubId, cancellationToken)
            ?? throw new BusinessException("The reservation does not exist.");
        if (reservation.ReservationStatusId == ReservationStatusIds.Cancelled)
            throw new BusinessException("Payments cannot be recorded for a canceled reservation.");
        if (reservation.Payments.Sum(x => x.Amount) + payment.Amount > reservation.FinalPrice)
            throw new ConflictException("The balance changed while the payment was being recorded. Review the outstanding balance.");

        payment.Reservation = reservation;
        payment.PaymentMethod = await dbContext.PaymentMethods.FirstAsync(x => x.Id == payment.PaymentMethodId, cancellationToken);
        await dbContext.Payments.AddAsync(payment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }
}
