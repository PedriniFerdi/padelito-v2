using System.ComponentModel.DataAnnotations;

namespace Padelito.Application.DTOs.Payments;

public sealed record PaymentFilterDto(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    int? MethodId = null,
    int? ReservationId = null);

public sealed record PaymentCreateDto(
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar una reserva.")] int ReservationId,
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un metodo de pago.")] int PaymentMethodId,
    [Range(0.01, 99999999.99, ErrorMessage = "El monto debe ser mayor a cero.")] decimal Amount,
    DateTime PaymentDate,
    [StringLength(255, ErrorMessage = "La nota no puede superar los 255 caracteres.")] string? Note);

public sealed record PaymentListDto(
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
    decimal TotalPaid,
    decimal PendingBalance);

public sealed record PaymentSummaryDto(decimal FinalPrice, decimal TotalPaid, decimal PendingBalance);
