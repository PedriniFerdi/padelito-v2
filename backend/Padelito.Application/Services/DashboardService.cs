using Padelito.Application.DTOs.Dashboard;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Application.Services;

public sealed class DashboardService(IDashboardRepository repository, TimeProvider timeProvider, TimeZoneInfo clubTimeZone) : IDashboardService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(int clubId, CancellationToken cancellationToken)
    {
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), clubTimeZone);
        var date = DateOnly.FromDateTime(localNow.DateTime);
        var localStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1);
        var fromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, clubTimeZone);
        var toUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, clubTimeZone);

        var clients = await repository.CountActiveClientsAsync(cancellationToken);
        var courts = await repository.CountActiveCourtsAsync(clubId, cancellationToken);
        var reservations = await repository.CountReservationsAsync(clubId, date, cancellationToken);
        var income = await repository.SumIncomeAsync(clubId, fromUtc, toUtc, cancellationToken);
        var latest = await repository.GetLatestReservationsAsync(clubId, date, 5, cancellationToken);
        return new(date, clients, courts, reservations, income, latest.Select(x => new DashboardReservationDto(
            x.Id, x.ReservationDate, $"{x.Client.Person.FirstName} {x.Client.Person.LastName}",
            x.AvailableTurn.Court.Name, x.AvailableTurn.StartTime, x.ReservationStatus.Name, x.FinalPrice)).ToList());
    }
}
