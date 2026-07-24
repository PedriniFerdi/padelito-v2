using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Padelito.Application.DTOs.Reservations;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize(Policy = "AdminOrReception")]
public sealed class ReservationsController(IReservationService reservationService) : CatalogControllerBase
{
    [HttpGet]
    public Task<ActionResult<IReadOnlyList<ReservationListDto>>> Get([FromQuery] ReservationFilterDto filter, CancellationToken cancellationToken) =>
        HandleAsync(() => reservationService.GetReservationsAsync(CurrentClubId, filter, cancellationToken));

    [HttpGet("availability")]
    public Task<ActionResult<IReadOnlyList<ReservationAvailabilityDto>>> GetAvailability([FromQuery] DateOnly date, CancellationToken cancellationToken) =>
        HandleAsync(() => reservationService.GetAvailabilityAsync(CurrentClubId, date, cancellationToken));

    [HttpGet("operations-board")]
    public Task<ActionResult<OperationsBoardDto>> GetOperationsBoard(CancellationToken cancellationToken) =>
        HandleAsync(() => reservationService.GetOperationsBoardAsync(CurrentClubId, cancellationToken));

    [HttpGet("{id:int}")]
    public Task<ActionResult<ReservationDetailDto>> GetById(int id, CancellationToken cancellationToken) =>
        HandleAsync(() => reservationService.GetReservationAsync(id, CurrentClubId, cancellationToken));

    [HttpPost]
    public Task<ActionResult<ReservationDetailDto>> Create(ReservationCreateDto request, CancellationToken cancellationToken) =>
        HandleAsync(() => reservationService.CreateAsync(CurrentClubId, CurrentEmployeeId, CurrentUsername, request, cancellationToken));

    [HttpPatch("{id:int}/status")]
    public Task<ActionResult<ReservationDetailDto>> ChangeStatus(int id, ReservationChangeStatusDto request, CancellationToken cancellationToken) =>
        HandleAsync(() => reservationService.ChangeStatusAsync(id, CurrentClubId, CurrentUsername, request, cancellationToken));

    private int CurrentEmployeeId => int.TryParse(User.FindFirstValue("EmployeeId"), out var value) ? value : throw new UnauthorizedAccessException();
    private string CurrentUsername => User.FindFirstValue(ClaimTypes.Name) ?? throw new UnauthorizedAccessException();
}
