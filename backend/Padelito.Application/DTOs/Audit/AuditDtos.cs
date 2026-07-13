namespace Padelito.Application.DTOs.Audit;

public sealed record ReservationAuditFilterDto(
    DateOnly? DateFrom = null,
    DateOnly? DateTo = null,
    int? ReservationId = null,
    string? Action = null,
    string? Username = null);

public sealed record ReservationAuditListDto(
    int Id,
    int ReservationId,
    DateOnly ReservationDate,
    string ClientName,
    string CourtName,
    string Action,
    string Description,
    string Username,
    DateTime CreatedAt);
