using Padelito.Application.Common;
using Padelito.Application.DTOs.Payments;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;
using Padelito.Domain.Entities;

namespace Padelito.Application.Services;

public sealed class PaymentService(IPaymentRepository repository) : IPaymentService
{
    public async Task<IReadOnlyList<PaymentListDto>> GetPaymentsAsync(int clubId, PaymentFilterDto filter, CancellationToken cancellationToken)
    {
        if (filter.DateFrom.HasValue && filter.DateTo.HasValue && filter.DateTo < filter.DateFrom)
            throw new BusinessException("La fecha hasta debe ser mayor o igual a la fecha desde.");

        var payments = await repository.GetPaymentsAsync(clubId, filter.DateFrom, filter.DateTo, filter.MethodId, filter.ReservationId, cancellationToken);
        return payments.Select(ToDto).ToList();
    }

    public async Task<PaymentListDto> CreateAsync(int clubId, PaymentCreateDto request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0) throw new BusinessException("El monto debe ser mayor a cero.");
        if (request.PaymentDate == default) throw new BusinessException("La fecha de pago es obligatoria.");
        var note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        if (note?.Length > 255) throw new BusinessException("La nota no puede superar los 255 caracteres.");

        var reservation = await repository.GetReservationAsync(request.ReservationId, clubId, cancellationToken)
            ?? throw new BusinessException("La reserva no existe.");
        if (reservation.ReservationStatusId == ReservationStatusIds.Cancelled)
            throw new BusinessException("No se pueden registrar pagos sobre una reserva cancelada.");

        var method = await repository.GetMethodAsync(request.PaymentMethodId, cancellationToken)
            ?? throw new BusinessException("El metodo de pago indicado no existe.");
        var totalPaid = reservation.Payments.Sum(x => x.Amount);
        if (totalPaid + request.Amount > reservation.FinalPrice)
            throw new BusinessException("El monto supera el saldo pendiente de la reserva.");

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
}

