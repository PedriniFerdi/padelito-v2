namespace Padelito.Application.Interfaces.Services;

public interface IReservationLifecycleService
{
    Task ReconcileAsync(int? clubId, CancellationToken cancellationToken);
}
