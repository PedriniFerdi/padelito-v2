using System.Globalization;
using System.Text;
using Padelito.Application.Common;
using Padelito.Application.DTOs.Reports;
using Padelito.Application.Interfaces.Repositories;
using Padelito.Application.Interfaces.Services;
using Padelito.Domain.Entities;

namespace Padelito.Application.Services;

public sealed class ReportService(IReportRepository repository) : IReportService
{
    public async Task<ReservationReportDto> GetReservationsAsync(int clubId, ReservationReportFilterDto filter, CancellationToken cancellationToken)
    {
        ValidateRange(filter.DateFrom, filter.DateTo);
        var reservations = await repository.GetReservationsAsync(clubId, filter.DateFrom, filter.DateTo, filter.StatusId, cancellationToken);
        var rows = reservations.Select(ToRow).ToList();
        return new ReservationReportDto(
            new ReservationReportSummaryDto(rows.Count, rows.Sum(x => x.FinalPrice), rows.Sum(x => x.TotalPaid), rows.Sum(x => x.PendingBalance)),
            rows);
    }

    public async Task<byte[]> ExportReservationsCsvAsync(int clubId, ReservationReportFilterDto filter, CancellationToken cancellationToken)
    {
        var report = await GetReservationsAsync(clubId, filter, cancellationToken);
        var csv = new StringBuilder();
        csv.AppendLine("Reserva;Fecha;Hora inicio;Hora fin;Cliente;Cancha;Estado;Promocion;Precio base;Precio final;Pagado;Saldo;Estado de pago");
        foreach (var row in report.Rows)
        {
            csv.AppendLine(string.Join(';', new[]
            {
                row.ReservationId.ToString(CultureInfo.InvariantCulture), row.ReservationDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                row.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture), row.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                Escape(row.ClientName), Escape(row.CourtName), Escape(row.Status), Escape(row.PromotionName ?? string.Empty),
                Decimal(row.BasePrice), Decimal(row.FinalPrice), Decimal(row.TotalPaid), Decimal(row.PendingBalance), Escape(row.PaymentStatus)
            }));
        }
        var encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
        return [.. encoding.GetPreamble(), .. encoding.GetBytes(csv.ToString())];
    }

    private static ReservationReportRowDto ToRow(Reservation reservation)
    {
        var paid = reservation.Payments.Sum(x => x.Amount);
        var balance = Math.Max(0, reservation.FinalPrice - paid);
        return new ReservationReportRowDto(
            reservation.Id, reservation.ReservationDate, reservation.AvailableTurn.StartTime, reservation.AvailableTurn.EndTime,
            $"{reservation.Client.Person.FirstName} {reservation.Client.Person.LastName}", reservation.AvailableTurn.Court.Name,
            reservation.ReservationStatusId, reservation.ReservationStatus.Name, reservation.Promotion?.Name,
            reservation.BasePrice, reservation.FinalPrice, paid, balance,
            paid <= 0 ? "Sin pagos" : balance > 0 ? "Pago parcial" : "Pagada");
    }

    private static void ValidateRange(DateOnly? from, DateOnly? to)
    {
        if (from.HasValue && to.HasValue && to < from)
            throw new BusinessException("La fecha hasta debe ser mayor o igual a la fecha desde.");
    }

    private static string Decimal(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Escape(string value) => value.Contains(';') || value.Contains('"') || value.Contains('\n')
        ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
