using Padelito.Domain.Entities;

namespace Padelito.Application.Interfaces.Repositories;

public interface IPaymentRepository
{
    Task<List<Payment>> GetPaymentsAsync(int clubId, DateOnly? dateFrom, DateOnly? dateTo, int? methodId, int? reservationId, CancellationToken cancellationToken);
    Task<Reservation?> GetReservationAsync(int id, int clubId, CancellationToken cancellationToken);
    Task<PaymentMethod?> GetMethodAsync(int id, CancellationToken cancellationToken);
    Task<Payment> AddPaymentAsync(int clubId, Payment payment, CancellationToken cancellationToken);
}

