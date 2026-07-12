using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Payments;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize(Policy = "AdminOrReception")]
public sealed class PaymentsController(IPaymentService paymentService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<PaymentListDto>>> Get([FromQuery] PaymentFilterDto filter, CancellationToken cancellationToken) =>
        HandleAsync(() => paymentService.GetPaymentsAsync(CurrentClubId, filter, cancellationToken));

    [HttpPost]
    public Task<ActionResult<PaymentListDto>> Create(PaymentCreateDto request, CancellationToken cancellationToken) =>
        HandleAsync(() => paymentService.CreateAsync(CurrentClubId, request, cancellationToken));
}
