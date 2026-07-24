using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Padelito.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class PrepareProductionData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "Payments",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "ReservationAudits",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "ReservationAudits",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "ReservationAudits",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "ReservationAudits",
                keyColumn: "Id",
                keyValue: 9004);

            migrationBuilder.DeleteData(
                table: "ReservationAudits",
                keyColumn: "Id",
                keyValue: 9005);

            migrationBuilder.DeleteData(
                table: "ReservationAudits",
                keyColumn: "Id",
                keyValue: 9006);

            migrationBuilder.DeleteData(
                table: "ReservationAudits",
                keyColumn: "Id",
                keyValue: 9007);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 9004);

            migrationBuilder.DeleteData(
                table: "Reservations",
                keyColumn: "Id",
                keyValue: 9005);

            migrationBuilder.DeleteData(
                table: "AvailableTurns",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "AvailableTurns",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "AvailableTurns",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "AvailableTurns",
                keyColumn: "Id",
                keyValue: 9004);

            migrationBuilder.DeleteData(
                table: "AvailableTurns",
                keyColumn: "Id",
                keyValue: 9005);

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "Clients",
                keyColumn: "Id",
                keyValue: 9004);

            migrationBuilder.DeleteData(
                table: "Employees",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Promotions",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "Courts",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "Courts",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "Courts",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "People",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "People",
                keyColumn: "Id",
                keyValue: 9001);

            migrationBuilder.DeleteData(
                table: "People",
                keyColumn: "Id",
                keyValue: 9002);

            migrationBuilder.DeleteData(
                table: "People",
                keyColumn: "Id",
                keyValue: 9003);

            migrationBuilder.DeleteData(
                table: "People",
                keyColumn: "Id",
                keyValue: 9004);

            migrationBuilder.DeleteData(
                table: "Clubs",
                keyColumn: "Id",
                keyValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Clubs",
                columns: new[] { "Id", "Address", "CreatedAt", "Email", "IsActive", "Name", "Phone" },
                values: new object[] { 1, "Buenos Aires", new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "admin@padelito.com", true, "Padelito", "11-4000-0000" });

            migrationBuilder.InsertData(
                table: "People",
                columns: new[] { "Id", "CreatedAt", "Dni", "Email", "FirstName", "IsActive", "LastName", "Phone" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "30111222", "carlos.benitez@padelito.com", "Carlos", true, "Benitez", "11-4000-1001" },
                    { 9001, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "35421678", "lucia.fernandez@example.com", "Lucía", true, "Fernández", "11-5821-4076" },
                    { 9002, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "32987412", "martin.sosa@example.com", "Martín", true, "Sosa", "11-4962-1180" },
                    { 9003, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "38741209", "valentina.rios@example.com", "Valentina", true, "Ríos", "11-6234-9041" },
                    { 9004, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), "36109874", "nicolas.acosta@example.com", "Nicolás", true, "Acosta", "11-4487-6620" }
                });

            migrationBuilder.InsertData(
                table: "Promotions",
                columns: new[] { "Id", "DateFrom", "DateTo", "Description", "DiscountPercentage", "IsActive", "Name" },
                values: new object[] { 9001, new DateOnly(2026, 7, 1), new DateOnly(2026, 12, 31), "Beneficio demo para time slots seleccionados.", 15m, true, "Horario tranquilo demo" });

            migrationBuilder.InsertData(
                table: "Clients",
                columns: new[] { "Id", "PersonId" },
                values: new object[,]
                {
                    { 9001, 9001 },
                    { 9002, 9002 },
                    { 9003, 9003 },
                    { 9004, 9004 }
                });

            migrationBuilder.InsertData(
                table: "Courts",
                columns: new[] { "Id", "ClubId", "CourtTypeId", "HourPrice", "IsActive", "Name" },
                values: new object[,]
                {
                    { 9001, 1, 2, 18000m, true, "Central Demo" },
                    { 9002, 1, 3, 22000m, true, "Norte Demo" },
                    { 9003, 1, 4, 26000m, true, "Arena Demo" }
                });

            migrationBuilder.InsertData(
                table: "Employees",
                columns: new[] { "Id", "ClubId", "PersonId" },
                values: new object[] { 1, 1, 1 });

            migrationBuilder.InsertData(
                table: "AvailableTurns",
                columns: new[] { "Id", "CourtId", "EndTime", "IsActive", "StartTime" },
                values: new object[,]
                {
                    { 9001, 9001, new TimeOnly(11, 30, 0), true, new TimeOnly(10, 0, 0) },
                    { 9002, 9001, new TimeOnly(19, 30, 0), true, new TimeOnly(18, 0, 0) },
                    { 9003, 9002, new TimeOnly(17, 30, 0), true, new TimeOnly(16, 0, 0) },
                    { 9004, 9002, new TimeOnly(21, 30, 0), true, new TimeOnly(20, 0, 0) },
                    { 9005, 9003, new TimeOnly(20, 30, 0), true, new TimeOnly(19, 0, 0) }
                });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "CreatedAt", "EmployeeId", "IsActive", "PasswordHash", "RoleId", "Username" },
                values: new object[] { 1, new DateTime(2026, 7, 8, 0, 0, 0, 0, DateTimeKind.Utc), 1, true, "AQAAAAIAAYagAAAAED2SFjyZfFosfjAmmH1n5FHdE59w+9e6K96p468HR/FvY6jo4v94M+pMCLf/9mpNhA==", 1, "admin" });

            migrationBuilder.InsertData(
                table: "Reservations",
                columns: new[] { "Id", "AvailableTurnId", "BasePrice", "ClientId", "CreatedAt", "EmployeeId", "FinalPrice", "PromotionId", "ReservationDate", "ReservationStatusId" },
                values: new object[,]
                {
                    { 9001, 9001, 27000m, 9001, new DateTime(2026, 7, 8, 10, 0, 0, 0, DateTimeKind.Utc), 1, 22950m, 9001, new DateOnly(2026, 7, 8), 4 },
                    { 9002, 9003, 33000m, 9002, new DateTime(2026, 7, 8, 12, 0, 0, 0, DateTimeKind.Utc), 1, 33000m, null, new DateOnly(2026, 7, 9), 4 },
                    { 9003, 9002, 27000m, 9003, new DateTime(2026, 7, 10, 14, 0, 0, 0, DateTimeKind.Utc), 1, 27000m, null, new DateOnly(2026, 7, 12), 2 },
                    { 9004, 9004, 33000m, 9004, new DateTime(2026, 7, 11, 16, 0, 0, 0, DateTimeKind.Utc), 1, 28050m, 9001, new DateOnly(2026, 7, 13), 1 },
                    { 9005, 9005, 39000m, 9001, new DateTime(2026, 7, 9, 9, 0, 0, 0, DateTimeKind.Utc), 1, 39000m, null, new DateOnly(2026, 7, 10), 3 }
                });

            migrationBuilder.InsertData(
                table: "Payments",
                columns: new[] { "Id", "Amount", "Note", "PaymentDate", "PaymentMethodId", "ReservationId" },
                values: new object[,]
                {
                    { 9001, 22950m, "Payment completo demo", new DateTime(2026, 7, 9, 11, 0, 0, 0, DateTimeKind.Utc), 4, 9001 },
                    { 9002, 15000m, "Seña en efectivo", new DateTime(2026, 7, 9, 13, 0, 0, 0, DateTimeKind.Utc), 1, 9002 },
                    { 9003, 27000m, "Venmo", new DateTime(2026, 7, 11, 15, 0, 0, 0, DateTimeKind.Utc), 5, 9003 }
                });

            migrationBuilder.InsertData(
                table: "ReservationAudits",
                columns: new[] { "Id", "Action", "CreatedAt", "Description", "ReservationId", "Username" },
                values: new object[,]
                {
                    { 9001, "Created", new DateTime(2026, 7, 8, 10, 0, 0, 0, DateTimeKind.Utc), "Reservation demo creada para Lucía Fernández en court Central Demo.", 9001, "admin" },
                    { 9002, "StatusChanged", new DateTime(2026, 7, 9, 12, 0, 0, 0, DateTimeKind.Utc), "Status cambiado de Confirmed a Completed.", 9001, "admin" },
                    { 9003, "Created", new DateTime(2026, 7, 8, 12, 0, 0, 0, DateTimeKind.Utc), "Reservation demo creada para Martín Sosa en court Norte Demo.", 9002, "admin" },
                    { 9004, "Created", new DateTime(2026, 7, 10, 14, 0, 0, 0, DateTimeKind.Utc), "Reservation demo creada para Valentina Ríos en court Central Demo.", 9003, "admin" },
                    { 9005, "Created", new DateTime(2026, 7, 11, 16, 0, 0, 0, DateTimeKind.Utc), "Reservation demo creada para Nicolás Acosta en court Norte Demo.", 9004, "admin" },
                    { 9006, "Created", new DateTime(2026, 7, 9, 9, 0, 0, 0, DateTimeKind.Utc), "Reservation demo creada para Lucía Fernández en court Arena Demo.", 9005, "admin" },
                    { 9007, "StatusChanged", new DateTime(2026, 7, 9, 10, 0, 0, 0, DateTimeKind.Utc), "Status cambiado de Pending a Canceled.", 9005, "admin" }
                });
        }
    }
}
