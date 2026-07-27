using Padelito.Domain.Entities;

namespace Padelito.Application.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<List<PaymentReadModel>> GetPaymentsAsync(int clubId, DateTime? dateFromUtc, DateTime? dateToExclusiveUtc, int? methodId, int? reservationId, CancellationToken cancellationToken);
    Task<Payment> AddFullPaymentAsync(
        int clubId,
        int reservationId,
        int paymentMethodId,
        string? note,
        string username,
        DateTime localNow,
        DateTime utcNow,
        CancellationToken cancellationToken);
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
