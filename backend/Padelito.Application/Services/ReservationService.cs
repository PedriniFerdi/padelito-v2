using Padelito.Application.Common;
using Padelito.Application.DTOs.Reservations;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;
using Padelito.Domain.Entities;

namespace Padelito.Application.Services;

public sealed class ReservationService(
    IReservationRepository repository,
    TimeProvider timeProvider,
    TimeZoneInfo clubTimeZone) : IReservationService
{
    public async Task<IReadOnlyList<ReservationListDto>> GetReservationsAsync(
        int clubId,
        ReservationFilterDto filter,
        CancellationToken cancellationToken)
    {
        var view = filter.View.Trim().ToLowerInvariant();
        var statuses = view switch
        {
            "active" => ReservationStatusIds.Active,
            "history" => ReservationStatusIds.History,
            _ => throw new BusinessException("La vista debe ser active o history.")
        };

        if (filter.DateFrom.HasValue && filter.DateTo.HasValue && filter.DateTo < filter.DateFrom)
        {
            throw new BusinessException("La fecha hasta debe ser mayor o igual a la fecha desde.");
        }

        if (filter.StatusId.HasValue && !statuses.Contains(filter.StatusId.Value))
        {
            throw new BusinessException("El estado no corresponde a la vista seleccionada.");
        }

        var reservations = await repository.GetReservationsAsync(
            clubId, statuses, filter.DateFrom, filter.DateTo, filter.StatusId, cancellationToken);
        return reservations.Select(ToListDto).ToList();
    }

    public async Task<ReservationDetailDto> GetReservationAsync(int id, int clubId, CancellationToken cancellationToken)
    {
        return ToDetailDto(await RequireReservationAsync(id, clubId, false, cancellationToken));
    }

    public async Task<IReadOnlyList<ReservationAvailabilityDto>> GetAvailabilityAsync(
        int clubId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        var now = GetLocalNow();
        var today = DateOnly.FromDateTime(now);
        if (date < today)
        {
            throw new BusinessException("No se puede consultar disponibilidad para una fecha pasada.");
        }

        var currentTime = TimeOnly.FromDateTime(now);
        var turns = await repository.GetAvailabilityAsync(clubId, date, cancellationToken);
        return turns
            .Where(turn => date > today || turn.StartTime > currentTime)
            .Select(turn => new ReservationAvailabilityDto(
                turn.Id,
                turn.CourtId,
                turn.Court.Name,
                turn.Court.CourtType.Description,
                turn.StartTime,
                turn.EndTime,
                CalculateBasePrice(turn)))
            .ToList();
    }

    public async Task<ReservationDetailDto> CreateAsync(
        int clubId,
        int employeeId,
        string username,
        ReservationCreateDto request,
        CancellationToken cancellationToken)
    {
        if (request.ReservationStatusId is not (ReservationStatusIds.Pending or ReservationStatusIds.Confirmed))
        {
            throw new BusinessException("La reserva debe crearse como Pendiente o Confirmada.");
        }

        ValidateFutureDate(request.ReservationDate);

        var client = await repository.GetClientAsync(request.ClientId, cancellationToken)
            ?? throw new BusinessException("El cliente indicado no existe.");
        if (!client.Person.IsActive)
        {
            throw new BusinessException("No se puede reservar para un cliente inactivo.");
        }

        var employee = await repository.GetEmployeeAsync(employeeId, cancellationToken)
            ?? throw new BusinessException("El empleado autenticado no existe.");
        if (!employee.Person.IsActive || employee.ClubId != clubId)
        {
            throw new BusinessException("El empleado autenticado no está activo en este club.");
        }

        var turn = await repository.GetAvailableTurnAsync(request.AvailableTurnId, cancellationToken)
            ?? throw new BusinessException("El turno indicado no existe.");
        if (!turn.IsActive || !turn.Court.IsActive)
        {
            throw new BusinessException("La cancha y el turno deben estar activos.");
        }

        if (turn.Court.ClubId != clubId)
        {
            throw new BusinessException("El turno no pertenece al club autenticado.");
        }

        ValidateFutureTime(request.ReservationDate, turn.StartTime);

        if (await repository.IsOccupiedAsync(request.ReservationDate, request.AvailableTurnId, cancellationToken))
        {
            throw new ConflictException("El turno ya está reservado para la fecha seleccionada.");
        }

        Promotion? promotion = null;
        if (request.PromotionId.HasValue)
        {
            promotion = await repository.GetPromotionAsync(request.PromotionId.Value, cancellationToken)
                ?? throw new BusinessException("La promoción indicada no existe.");
            if (!promotion.IsActive || request.ReservationDate < promotion.DateFrom || request.ReservationDate > promotion.DateTo)
            {
                throw new BusinessException("La promoción no está activa y vigente para la fecha de reserva.");
            }
        }

        var status = await repository.GetStatusAsync(request.ReservationStatusId, cancellationToken)
            ?? throw new BusinessException("El estado indicado no existe.");
        var basePrice = CalculateBasePrice(turn);
        var finalPrice = CalculateFinalPrice(basePrice, promotion?.DiscountPercentage);
        var reservation = new Reservation
        {
            ClientId = client.Id,
            Client = client,
            AvailableTurnId = turn.Id,
            AvailableTurn = turn,
            EmployeeId = employee.Id,
            Employee = employee,
            PromotionId = promotion?.Id,
            Promotion = promotion,
            ReservationDate = request.ReservationDate,
            ReservationStatusId = status.Id,
            ReservationStatus = status,
            BasePrice = basePrice,
            FinalPrice = finalPrice,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        };
        reservation.Audits.Add(new ReservationAudit
        {
            Reservation = reservation,
            Action = "Creacion",
            Description = $"Reserva creada en estado {status.Name} para {client.Person.FirstName} {client.Person.LastName}, cancha {turn.Court.Name}, turno {turn.StartTime:HH\\:mm}-{turn.EndTime:HH\\:mm}.",
            Username = username,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        });

        await repository.AddAsync(reservation, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ToDetailDto(reservation);
    }

    public async Task<ReservationDetailDto> ChangeStatusAsync(
        int id,
        int clubId,
        string username,
        ReservationChangeStatusDto request,
        CancellationToken cancellationToken)
    {
        var reservation = await RequireReservationAsync(id, clubId, true, cancellationToken);
        if (!IsValidTransition(reservation.ReservationStatusId, request.ReservationStatusId))
        {
            throw new BusinessException("El cambio de estado solicitado no está permitido.");
        }

        var newStatus = await repository.GetStatusAsync(request.ReservationStatusId, cancellationToken)
            ?? throw new BusinessException("El estado indicado no existe.");
        var previousStatus = reservation.ReservationStatus.Name;
        reservation.ReservationStatusId = newStatus.Id;
        reservation.ReservationStatus = newStatus;
        reservation.Audits.Add(new ReservationAudit
        {
            ReservationId = reservation.Id,
            Action = "CambioEstado",
            Description = $"Estado cambiado de {previousStatus} a {newStatus.Name}.",
            Username = username,
            CreatedAt = timeProvider.GetUtcNow().UtcDateTime
        });

        await repository.SaveChangesAsync(cancellationToken);
        return ToDetailDto(reservation);
    }

    private async Task<Reservation> RequireReservationAsync(int id, int clubId, bool trackChanges, CancellationToken cancellationToken)
    {
        return await repository.GetReservationAsync(id, clubId, trackChanges, cancellationToken)
            ?? throw new BusinessException("La reserva no existe.");
    }

    private DateTime GetLocalNow()
    {
        return TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), clubTimeZone).DateTime;
    }

    private void ValidateFutureDate(DateOnly date)
    {
        if (date < DateOnly.FromDateTime(GetLocalNow()))
        {
            throw new BusinessException("No se puede crear una reserva para una fecha pasada.");
        }
    }

    private void ValidateFutureTime(DateOnly date, TimeOnly startTime)
    {
        var now = GetLocalNow();
        if (date == DateOnly.FromDateTime(now) && startTime <= TimeOnly.FromDateTime(now))
        {
            throw new BusinessException("No se puede reservar un turno cuyo horario ya comenzó.");
        }
    }

    private static bool IsValidTransition(int currentStatusId, int newStatusId)
    {
        return currentStatusId switch
        {
            ReservationStatusIds.Pending => newStatusId is ReservationStatusIds.Confirmed or ReservationStatusIds.Cancelled,
            ReservationStatusIds.Confirmed => newStatusId is ReservationStatusIds.Completed or ReservationStatusIds.Cancelled,
            _ => false
        };
    }

    private static decimal CalculateBasePrice(AvailableTurn turn)
    {
        var durationMinutes = (decimal)(turn.EndTime - turn.StartTime).TotalMinutes;
        return decimal.Round(turn.Court.HourPrice * durationMinutes / 60m, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal CalculateFinalPrice(decimal basePrice, decimal? discountPercentage)
    {
        var multiplier = 1m - (discountPercentage ?? 0m) / 100m;
        return decimal.Round(basePrice * multiplier, 2, MidpointRounding.AwayFromZero);
    }

    private static ReservationListDto ToListDto(Reservation reservation)
    {
        return new ReservationListDto(
            reservation.Id,
            reservation.ReservationDate,
            reservation.ClientId,
            FullName(reservation.Client.Person),
            reservation.AvailableTurnId,
            reservation.AvailableTurn.Court.Name,
            reservation.AvailableTurn.StartTime,
            reservation.AvailableTurn.EndTime,
            reservation.ReservationStatusId,
            reservation.ReservationStatus.Name,
            reservation.Promotion?.Name,
            reservation.BasePrice,
            reservation.FinalPrice,
            reservation.CreatedAt);
    }

    private static ReservationDetailDto ToDetailDto(Reservation reservation)
    {
        return new ReservationDetailDto(
            reservation.Id,
            reservation.ReservationDate,
            reservation.ClientId,
            FullName(reservation.Client.Person),
            reservation.AvailableTurnId,
            reservation.AvailableTurn.CourtId,
            reservation.AvailableTurn.Court.Name,
            reservation.AvailableTurn.Court.CourtType.Description,
            reservation.AvailableTurn.StartTime,
            reservation.AvailableTurn.EndTime,
            reservation.EmployeeId,
            FullName(reservation.Employee.Person),
            reservation.PromotionId,
            reservation.Promotion?.Name,
            reservation.Promotion?.DiscountPercentage,
            reservation.ReservationStatusId,
            reservation.ReservationStatus.Name,
            reservation.BasePrice,
            reservation.FinalPrice,
            reservation.CreatedAt);
    }

    private static string FullName(Person person) => $"{person.FirstName} {person.LastName}";
}
