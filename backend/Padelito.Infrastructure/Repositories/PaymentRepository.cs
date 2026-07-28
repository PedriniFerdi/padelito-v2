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

    public async Task<Payment> AddFullPaymentAsync(
        int clubId,
        int reservationId,
        int paymentMethodId,
        string? note,
        string username,
        DateTime localNow,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var reservation = await dbContext.Reservations.Include(x => x.Payments).Include(x => x.Client).ThenInclude(x => x.Person)
            .Include(x => x.AvailableTurn).ThenInclude(x => x.Court)
            .FirstOrDefaultAsync(x => x.Id == reservationId && x.AvailableTurn.Court.ClubId == clubId, cancellationToken)
            ?? throw new BusinessException("The reservation does not exist.");

        if (reservation.ReservationStatusId is not (ReservationStatusIds.Pending or ReservationStatusIds.Confirmed))
            throw new BusinessException("Only active reservations can be paid.");
        var start = reservation.ReservationDate.ToDateTime(reservation.AvailableTurn.StartTime);
        if (localNow >= start)
            throw new BusinessException("Payments cannot be recorded after the time slot has started.");
        if (reservation.Payments.Count != 0)
            throw new ConflictException("This reservation already has a recorded payment.");

        var method = await dbContext.PaymentMethods.FirstOrDefaultAsync(
            x => x.Id == paymentMethodId, cancellationToken)
            ?? throw new BusinessException("The selected payment method does not exist.");
        var payment = new Payment
        {
            ReservationId = reservation.Id,
            Reservation = reservation,
            PaymentMethodId = method.Id,
            PaymentMethod = method,
            Amount = reservation.FinalPrice,
            PaymentDate = utcNow,
            Note = note
        };
        reservation.Payments.Add(payment);
        if (reservation.ReservationStatusId == ReservationStatusIds.Pending)
        {
            reservation.ReservationStatusId = ReservationStatusIds.Confirmed;
            reservation.Audits.Add(new ReservationAudit
            {
                ReservationId = reservation.Id,
                Action = "PaymentConfirmed",
                Description = $"Full payment of ${reservation.FinalPrice:N2} recorded; reservation confirmed automatically.",
                Username = username,
                CreatedAt = utcNow
            });
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.GetBaseException() is Microsoft.Data.SqlClient.SqlException { Number: 1205 or 2601 or 2627 })
        {
            throw new ConflictException("This reservation already has a recorded payment.");
        }
        await transaction.CommitAsync(cancellationToken);
        return payment;
    }
}
