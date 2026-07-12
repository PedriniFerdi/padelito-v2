using Padelito.Application.DTOs.Payments;

namespace Padelito.Application.Interfaces.Services;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentListDto>> GetPaymentsAsync(int clubId, PaymentFilterDto filter, CancellationToken cancellationToken);
    Task<PaymentListDto> CreateAsync(int clubId, PaymentCreateDto request, CancellationToken cancellationToken);
}

