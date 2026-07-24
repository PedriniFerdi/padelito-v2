using Padelito.Application.Common;
using Padelito.Application.DTOs.Payments;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;
using Padelito.Domain.Entities;

namespace Padelito.Application.Services;

public sealed class PaymentService(IPaymentRepository repository, TimeZoneInfo clubTimeZone) : IPaymentService
{
    public async Task<IReadOnlyList<PaymentListDto>> GetPaymentsAsync(int clubId, PaymentFilterDto filter, CancellationToken cancellationToken)
    {
        if (filter.DateFrom.HasValue && filter.DateTo.HasValue && filter.DateTo < filter.DateFrom)
            throw new BusinessException("End date must be on or after start date.");

        DateTime? dateFromUtc = filter.DateFrom.HasValue ? ToUtc(filter.DateFrom.Value) : null;
        DateTime? dateToExclusiveUtc = filter.DateTo.HasValue ? ToUtc(filter.DateTo.Value.AddDays(1)) : null;
        var payments = await repository.GetPaymentsAsync(clubId, dateFromUtc, dateToExclusiveUtc, filter.MethodId, filter.ReservationId, cancellationToken);
        return payments.Select(ToDto).ToList();
    }

    public async Task<PaymentListDto> CreateAsync(int clubId, PaymentCreateDto request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0) throw new BusinessException("Amount must be greater than zero.");
        if (request.PaymentDate == default) throw new BusinessException("Payment date is required.");
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note?.Length > 255) throw new BusinessException("Note cannot exceed 255 characters.");

        var reservation = await repository.GetReservationAsync(request.ReservationId, clubId, cancellationToken)
            ?? throw new BusinessException("The reservation does not exist.");
        if (reservation.ReservationStatusId == ReservationStatusIds.Cancelled)
            throw new BusinessException("Payments cannot be recorded for a canceled reservation.");

        var method = await repository.GetMethodAsync(request.PaymentMethodId, cancellationToken)
            ?? throw new BusinessException("The selected payment method does not exist.");
        var totalPaid = reservation.Payments.Sum(x => x.Amount);
        if (totalPaid + request.Amount > reservation.FinalPrice)
            throw new BusinessException("Amount exceeds the reservation outstanding balance.");

        var payment = new Payment
        {
            ReservationId = reservation.Id,
            Reservation = reservation,
            PaymentMethodId = method.Id,
            PaymentMethod = method,
            Amount = decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero),
            PaymentDate = request.PaymentDate,
            Note = note
        };
        return ToDto(await repository.AddPaymentAsync(clubId, payment, cancellationToken));
    }

    private static PaymentListDto ToDto(Payment payment)
    {
        var paid = payment.Reservation.Payments.Sum(x => x.Amount);
        return new(payment.Id, payment.ReservationId, payment.Reservation.ReservationDate,
            $"{payment.Reservation.Client.Person.FirstName} {payment.Reservation.Client.Person.LastName}",
            payment.Reservation.AvailableTurn.Court.Name, payment.PaymentMethodId, payment.PaymentMethod.Description,
            payment.Amount, payment.PaymentDate, payment.Note, payment.Reservation.FinalPrice, paid,
            Math.Max(0, payment.Reservation.FinalPrice - paid));
    }

    private static PaymentListDto ToDto(PaymentReadModel payment) =>
        new(payment.Id, payment.ReservationId, payment.ReservationDate, payment.ClientName,
            payment.CourtName, payment.PaymentMethodId, payment.PaymentMethod, payment.Amount,
            payment.PaymentDate, payment.Note, payment.FinalPrice, payment.TotalPaid,
            Math.Max(0, payment.FinalPrice - payment.TotalPaid));

    private DateTime ToUtc(DateOnly date)
    {
        var localMidnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(localMidnight, clubTimeZone);
    }
}
