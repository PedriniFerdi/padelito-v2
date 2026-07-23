using Padelito.Application.DTOs.Dashboard;

namespace Padelito.Application.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(int clubId, CancellationToken cancellationToken);
    Task<DashboardRevenueIntelligenceDto> GetRevenueIntelligenceAsync(int clubId, DashboardRevenueIntelligenceFilterDto filter, CancellationToken cancellationToken);
}
