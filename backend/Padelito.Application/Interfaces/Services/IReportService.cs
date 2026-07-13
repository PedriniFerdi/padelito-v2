using Padelito.Application.DTOs.Reports;

namespace Padelito.Application.Interfaces.Services;

public interface IReportService
{
    Task<ReservationReportDto> GetReservationsAsync(int clubId, ReservationReportFilterDto filter, CancellationToken cancellationToken);
    Task<byte[]> ExportReservationsCsvAsync(int clubId, ReservationReportFilterDto filter, CancellationToken cancellationToken);
}
