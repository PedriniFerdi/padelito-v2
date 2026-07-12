using System.Data;
using Microsoft.EntityFrameworkCore;
using Padelito.Application.Common;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Domain.Entities;
using Padelito.Infrastructure.Data;

namespace Padelito.Infrastructure.Repositories;

public sealed class PaymentRepository(PadelitoDbContext dbContext) : IPaymentRepository
{
    public Task<List<Payment>> GetPaymentsAsync(int clubId, DateOnly? dateFrom, DateOnly? dateTo, int? methodId, int? reservationId, CancellationToken cancellationToken)
    {
        var query = Details().Where(x => x.Reservation.AvailableTurn.Court.ClubId == clubId);
        if (dateFrom.HasValue) query = query.Where(x => x.PaymentDate >= dateFrom.Value.ToDateTime(TimeOnly.MinValue));
        if (dateTo.HasValue) query = query.Where(x => x.PaymentDate < dateTo.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
        if (methodId.HasValue) query = query.Where(x => x.PaymentMethodId == methodId.Value);
        if (reservationId.HasValue) query = query.Where(x => x.ReservationId == reservationId.Value);
        return query.AsNoTracking().OrderByDescending(x => x.PaymentDate).ThenByDescending(x => x.Id).ToListAsync(cancellationToken);
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
            ?? throw new BusinessException("La reserva no existe.");
        if (reservation.ReservationStatusId == ReservationStatusIds.Cancelled)
            throw new BusinessException("No se pueden registrar pagos sobre una reserva cancelada.");
        if (reservation.Payments.Sum(x => x.Amount) + payment.Amount > reservation.FinalPrice)
            throw new ConflictException("El saldo cambio mientras se registraba el pago. Revise el saldo pendiente.");

        payment.Reservation = reservation;
        payment.PaymentMethod = await dbContext.PaymentMethods.FirstAsync(x => x.Id == payment.PaymentMethodId, cancellationToken);
        await dbContext.Payments.AddAsync(payment, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }

    private IQueryable<Payment> Details() => dbContext.Payments.Include(x => x.PaymentMethod)
        .Include(x => x.Reservation).ThenInclude(x => x.Payments)
        .Include(x => x.Reservation).ThenInclude(x => x.Client).ThenInclude(x => x.Person)
        .Include(x => x.Reservation).ThenInclude(x => x.AvailableTurn).ThenInclude(x => x.Court);
}
