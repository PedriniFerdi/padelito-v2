using Padelito.Application.Common;
using Padelito.Application.DTOs.Payments;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;
using Padelito.Domain.Entities;

namespace Padelito.Application.Services;

public sealed class PaymentService(
    IPaymentRepository repository,
    IReservationLifecycleService lifecycleService,
    TimeProvider timeProvider,
    TimeZoneInfo clubTimeZone) : IPaymentService
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

    public async Task<PaymentListDto> CreateAsync(
        int clubId,
        string username,
        PaymentCreateDto request,
        CancellationToken cancellationToken)
    {
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note?.Length > 255) throw new BusinessException("Note cannot exceed 255 characters.");

        await lifecycleService.ReconcileAsync(clubId, cancellationToken);
        var utcNow = timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(utcNow, clubTimeZone).DateTime;
        return ToDto(await repository.AddFullPaymentAsync(
            clubId,
            request.ReservationId,
            request.PaymentMethodId,
            note,
            username,
            localNow,
            utcNow.UtcDateTime,
            cancellationToken));
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
