using Padelito.Domain.Entities;

namespace Padelito.Application.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<List<PaymentReadModel>> GetPaymentsAsync(int clubId, DateTime? dateFromUtc, DateTime? dateToExclusiveUtc, int? methodId, int? reservationId, CancellationToken cancellationToken);
    Task<Reservation?> GetReservationAsync(int id, int clubId, CancellationToken cancellationToken);
    Task<PaymentMethod?> GetMethodAsync(int id, CancellationToken cancellationToken);
    Task<Payment> AddPaymentAsync(int clubId, Payment payment, CancellationToken cancellationToken);
}

public sealed record PaymentReadModel(
    int Id,
    int ReservationId,
    DateOnly ReservationDate,
    string ClientName,
    string CourtName,
    int PaymentMethodId,
    string PaymentMethod,
    decimal Amount,
    DateTime PaymentDate,
    string? Note,
    decimal FinalPrice,
    decimal TotalPaid);
