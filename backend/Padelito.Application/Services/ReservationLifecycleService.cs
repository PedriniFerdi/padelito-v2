using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;

namespace Padelito.Application.Services;

public sealed class ReservationLifecycleService(
    IReservationLifecycleRepository repository,
    TimeProvider timeProvider,
    TimeZoneInfo clubTimeZone) : IReservationLifecycleService
{
    public Task ReconcileAsync(int? clubId, CancellationToken cancellationToken)
    {
        var utcNow = timeProvider.GetUtcNow();
        var localNow = TimeZoneInfo.ConvertTime(utcNow, clubTimeZone).DateTime;
        return repository.ReconcileAsync(clubId, localNow, utcNow.UtcDateTime, cancellationToken);
    }
}
