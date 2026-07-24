using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Padelito.Infrastructure.Data;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations;

[DbContext(typeof(PadelitoDbContext))]
[Migration("20260724160000_LocalizeDemoDataForUs")]
public sealed class LocalizeDemoDataForUs : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE [Roles] SET [Name] = CASE [Id]
                WHEN 1 THEN N'Admin'
                WHEN 2 THEN N'Reception'
                WHEN 3 THEN N'Staff'
            END
            WHERE [Id] IN (1, 2, 3);

            UPDATE [ReservationStatuses] SET [Name] = CASE [Id]
                WHEN 1 THEN N'Pending'
                WHEN 2 THEN N'Confirmed'
                WHEN 3 THEN N'Canceled'
                WHEN 4 THEN N'Completed'
            END
            WHERE [Id] IN (1, 2, 3, 4);

            UPDATE [PaymentMethods] SET [Description] = CASE [Id]
                WHEN 1 THEN N'Cash'
                WHEN 2 THEN N'Debit Card'
                WHEN 3 THEN N'Credit Card'
                WHEN 4 THEN N'Bank Transfer'
                WHEN 5 THEN N'Venmo'
            END
            WHERE [Id] IN (1, 2, 3, 4, 5);

            UPDATE [CourtTypes] SET [Description] = CASE [Id]
                WHEN 1 THEN N'Concrete'
                WHEN 2 THEN N'Synthetic Turf'
                WHEN 3 THEN N'Indoor'
                WHEN 4 THEN N'Premium'
            END
            WHERE [Id] IN (1, 2, 3, 4);
            """);

        migrationBuilder.Sql(
            """
            CREATE OR ALTER TRIGGER [dbo].[TR_AvailableTurns_PreventOverlap]
            ON [dbo].[AvailableTurns]
            AFTER INSERT, UPDATE
            AS
            BEGIN
                SET NOCOUNT ON;

                IF NOT EXISTS (SELECT 1 FROM inserted WHERE [IsActive] = 1)
                    RETURN;

                DECLARE @lockedCourts int;
                SELECT @lockedCourts = COUNT(*)
                FROM [dbo].[Courts] WITH (UPDLOCK, HOLDLOCK)
                WHERE [Id] IN
                (
                    SELECT DISTINCT [CourtId]
                    FROM inserted
                    WHERE [IsActive] = 1
                );

                IF EXISTS
                (
                    SELECT 1
                    FROM inserted AS candidate
                    INNER JOIN [dbo].[AvailableTurns] AS existing
                        ON existing.[CourtId] = candidate.[CourtId]
                       AND existing.[Id] <> candidate.[Id]
                       AND existing.[IsActive] = 1
                       AND candidate.[IsActive] = 1
                       AND candidate.[StartTime] < existing.[EndTime]
                       AND candidate.[EndTime] > existing.[StartTime]
                )
                BEGIN
                    ;THROW 51001, 'The time slot overlaps another active slot for the same court.', 1;
                END;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE [Roles] SET [Name] = CASE [Id]
                WHEN 1 THEN N'Administrador'
                WHEN 2 THEN N'Recepcion'
                WHEN 3 THEN N'Empleado'
            END
            WHERE [Id] IN (1, 2, 3);

            UPDATE [ReservationStatuses] SET [Name] = CASE [Id]
                WHEN 1 THEN N'Pendiente'
                WHEN 2 THEN N'Confirmada'
                WHEN 3 THEN N'Cancelada'
                WHEN 4 THEN N'Finalizada'
            END
            WHERE [Id] IN (1, 2, 3, 4);

            UPDATE [PaymentMethods] SET [Description] = CASE [Id]
                WHEN 1 THEN N'Efectivo'
                WHEN 2 THEN N'Tarjeta de debito'
                WHEN 3 THEN N'Tarjeta de credito'
                WHEN 4 THEN N'Transferencia'
                WHEN 5 THEN N'Mercado Pago'
            END
            WHERE [Id] IN (1, 2, 3, 4, 5);

            UPDATE [CourtTypes] SET [Description] = CASE [Id]
                WHEN 1 THEN N'Cemento'
                WHEN 2 THEN N'Sintetico'
                WHEN 3 THEN N'Indoor'
                WHEN 4 THEN N'Premium'
            END
            WHERE [Id] IN (1, 2, 3, 4);
            """);
    }
}
