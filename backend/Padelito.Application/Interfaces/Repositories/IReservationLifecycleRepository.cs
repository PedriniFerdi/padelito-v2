namespace Padelito.Application.Interfaces.Repositories;

public interface IReservationLifecycleRepository
{
    Task ReconcileAsync(
        int? clubId,
        DateTime localNow,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
