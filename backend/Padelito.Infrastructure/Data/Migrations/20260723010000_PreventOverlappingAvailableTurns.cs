using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Padelito.Infrastructure.Data;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations;

[DbContext(typeof(PadelitoDbContext))]
[Migration("20260723010000_PreventOverlappingAvailableTurns")]
public sealed class PreventOverlappingAvailableTurns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Keep the oldest active turn in each overlap and deactivate later conflicting
        // rows. Reservations and their history remain untouched.
        migrationBuilder.Sql(
            """
            UPDATE laterTurn
            SET [IsActive] = 0
            FROM [AvailableTurns] AS laterTurn
            WHERE laterTurn.[IsActive] = 1
              AND EXISTS
              (
                  SELECT 1
                  FROM [AvailableTurns] AS earlierTurn
                  WHERE earlierTurn.[CourtId] = laterTurn.[CourtId]
                    AND earlierTurn.[IsActive] = 1
                    AND earlierTurn.[Id] < laterTurn.[Id]
                    AND earlierTurn.[StartTime] < laterTurn.[EndTime]
                    AND earlierTurn.[EndTime] > laterTurn.[StartTime]
              );
            """);

        // The controller/service validation provides a friendly response. This trigger
        // is the final guarantee for concurrent requests, multiple app instances and
        // direct SQL writes. The court row lock serializes schedule changes per court.
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
                    ;THROW 51001, 'El horario se superpone con otro time slot activo de la misma court.', 1;
                END;
            END;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP TRIGGER IF EXISTS [dbo].[TR_AvailableTurns_PreventOverlap];");
    }
}
