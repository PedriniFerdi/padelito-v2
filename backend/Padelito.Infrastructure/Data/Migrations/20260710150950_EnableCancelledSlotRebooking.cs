using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class EnableCancelledSlotRebooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_ReservationDate_AvailableTurnId",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ReservationDate_AvailableTurnId",
                table: "Reservations",
                columns: new[] { "ReservationDate", "AvailableTurnId" },
                unique: true,
                filter: "[ReservationStatusId] <> 3");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Reservations_ReservationDate_AvailableTurnId",
                table: "Reservations");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_ReservationDate_AvailableTurnId",
                table: "Reservations",
                columns: new[] { "ReservationDate", "AvailableTurnId" },
                unique: true);
        }
    }
}
