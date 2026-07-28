using Padelito.Application.Interfaces.Services;

namespace Padelito.Api.Startup;

public sealed class ReservationLifecycleWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ReservationLifecycleWorker> logger) : BackgroundService
{
    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        await ReconcileAsync(cancellationToken);
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await ReconcileAsync(stoppingToken);
            }
            catch (Exception exception) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogError(exception, "Reservation lifecycle reconciliation failed.");
            }
        }
    }

    private async Task ReconcileAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<IReservationLifecycleService>();
        await service.ReconcileAsync(null, cancellationToken);
    }
}
