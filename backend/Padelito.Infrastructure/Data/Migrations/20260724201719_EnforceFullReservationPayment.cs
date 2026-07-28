using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnforceFullReservationPayment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS (
                    SELECT 1
                    FROM [Payments] AS payment
                    INNER JOIN [Reservations] AS reservation
                        ON reservation.[Id] = payment.[ReservationId]
                    GROUP BY payment.[ReservationId], reservation.[FinalPrice]
                    HAVING COUNT_BIG(*) <> 1
                        OR SUM(CONVERT(decimal(38, 2), payment.[Amount]))
                            <> CONVERT(decimal(38, 2), reservation.[FinalPrice])
                )
                BEGIN
                    ;THROW 51003, 'Payment history is incompatible with the one-full-payment model. Run the PRD-BLK-003 preflight; no payment records were changed.', 1;
                END;
                """);

            migrationBuilder.DropIndex(
                name: "IX_Payments_ReservationId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReservationId",
                table: "Payments",
                column: "ReservationId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ReservationId",
                table: "Payments");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReservationId",
                table: "Payments",
                column: "ReservationId");
        }
    }
}
