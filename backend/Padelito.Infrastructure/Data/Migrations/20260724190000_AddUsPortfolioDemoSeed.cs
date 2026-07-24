using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Padelito.Infrastructure.Data;

#nullable disable

namespace Padelito.Infrastructure.Data.Migrations;

[DbContext(typeof(PadelitoDbContext))]
[Migration("20260724190000_AddUsPortfolioDemoSeed")]
public sealed class AddUsPortfolioDemoSeed : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DECLARE @adminRoleId int = (SELECT TOP (1) [Id] FROM [Roles] WHERE [Name] = N'Admin' ORDER BY [Id]);

            IF @adminRoleId IS NULL
                THROW 51010, 'Create the Admin role before applying the portfolio demo seed.', 1;

            DECLARE @clubId int = (SELECT TOP (1) [Id] FROM [Clubs] WHERE [IsActive] = 1 ORDER BY [Id]);

            IF @clubId IS NULL
            BEGIN
                INSERT INTO [Clubs] ([Name], [Address], [Phone], [Email], [IsActive], [CreatedAt])
                VALUES (N'Padelito NYC', N'New York, NY', N'(212) 555-0199', N'admin@padelito.com', 1, '2026-07-24T12:00:00Z');

                SET @clubId = CONVERT(int, SCOPE_IDENTITY());
            END;

            DECLARE @employeeId int =
            (
                SELECT TOP (1) [EmployeeId]
                FROM [Users]
                WHERE [IsActive] = 1
                ORDER BY CASE WHEN [Username] = N'admin' THEN 0 ELSE 1 END, [Id]
            );

            IF @employeeId IS NULL
            BEGIN
                INSERT INTO [People] ([FirstName], [LastName], [Dni], [Phone], [Email], [IsActive], [CreatedAt])
                VALUES (N'Alex', N'Morgan', N'555019999', N'(212) 555-0199', N'admin@padelito.com', 1, '2026-07-24T12:00:00Z');

                DECLARE @personId int = CONVERT(int, SCOPE_IDENTITY());

                INSERT INTO [Employees] ([PersonId], [ClubId])
                VALUES (@personId, @clubId);

                SET @employeeId = CONVERT(int, SCOPE_IDENTITY());

                INSERT INTO [Users] ([Username], [PasswordHash], [EmployeeId], [RoleId], [IsActive], [CreatedAt])
                VALUES (N'admin', N'AQAAAAIAAYagAAAAED2SFjyZfFosfjAmmH1n5FHdE59w+9e6K96p468HR/FvY6jo4v94M+pMCLf/9mpNhA==', @employeeId, @adminRoleId, 1, '2026-07-24T12:00:00Z');
            END;

            SET IDENTITY_INSERT [Courts] ON;
            INSERT INTO [Courts] ([Id], [ClubId], [CourtTypeId], [Name], [HourPrice], [IsActive])
            VALUES
                (9101, @clubId, 2, N'Hudson Court', 48.00, 1),
                (9102, @clubId, 3, N'Brooklyn Indoor', 62.00, 1),
                (9103, @clubId, 4, N'Manhattan Premium', 78.00, 1);
            SET IDENTITY_INSERT [Courts] OFF;

            SET IDENTITY_INSERT [People] ON;
            INSERT INTO [People] ([Id], [FirstName], [LastName], [Dni], [Phone], [Email], [IsActive], [CreatedAt])
            VALUES
                (9101, N'Maya', N'Johnson', N'555010101', N'(212) 555-0101', N'maya.johnson@example.com', 1, '2026-07-01T13:00:00Z'),
                (9102, N'Ethan', N'Brooks', N'555010102', N'(212) 555-0102', N'ethan.brooks@example.com', 1, '2026-07-02T14:00:00Z'),
                (9103, N'Sofia', N'Martinez', N'555010103', N'(718) 555-0103', N'sofia.martinez@example.com', 1, '2026-07-03T15:00:00Z'),
                (9104, N'Liam', N'Carter', N'555010104', N'(646) 555-0104', N'liam.carter@example.com', 1, '2026-07-04T16:00:00Z'),
                (9105, N'Ava', N'Thompson', N'555010105', N'(917) 555-0105', N'ava.thompson@example.com', 1, '2026-07-05T17:00:00Z');
            SET IDENTITY_INSERT [People] OFF;

            SET IDENTITY_INSERT [Clients] ON;
            INSERT INTO [Clients] ([Id], [PersonId])
            VALUES (9101, 9101), (9102, 9102), (9103, 9103), (9104, 9104), (9105, 9105);
            SET IDENTITY_INSERT [Clients] OFF;

            SET IDENTITY_INSERT [Promotions] ON;
            INSERT INTO [Promotions] ([Id], [Name], [Description], [DiscountPercentage], [DateFrom], [DateTo], [IsActive])
            VALUES
                (9101, N'Weekday Off-Peak', N'15% off selected weekday reservations.', 15.00, '2026-07-01', '2026-12-31', 1),
                (9102, N'First Match', N'10% welcome discount for new customers.', 10.00, '2026-07-01', '2026-12-31', 1);
            SET IDENTITY_INSERT [Promotions] OFF;

            SET IDENTITY_INSERT [AvailableTurns] ON;
            INSERT INTO [AvailableTurns] ([Id], [CourtId], [StartTime], [EndTime], [IsActive])
            VALUES
                (9101, 9101, '09:00:00', '10:30:00', 1),
                (9102, 9101, '18:00:00', '19:30:00', 1),
                (9103, 9102, '11:00:00', '12:30:00', 1),
                (9104, 9102, '19:30:00', '21:00:00', 1),
                (9105, 9103, '16:00:00', '17:30:00', 1),
                (9106, 9103, '21:00:00', '22:30:00', 1);
            SET IDENTITY_INSERT [AvailableTurns] OFF;

            SET IDENTITY_INSERT [Reservations] ON;
            INSERT INTO [Reservations]
                ([Id], [ClientId], [AvailableTurnId], [EmployeeId], [PromotionId], [ReservationDate],
                 [ReservationStatusId], [BasePrice], [FinalPrice], [CreatedAt])
            VALUES
                (9101, 9101, 9101, @employeeId, 9101, '2026-07-24', 4, 72.00, 61.20, '2026-07-22T14:00:00Z'),
                (9102, 9102, 9103, @employeeId, NULL, '2026-07-24', 2, 93.00, 93.00, '2026-07-22T15:00:00Z'),
                (9103, 9103, 9102, @employeeId, NULL, '2026-07-24', 1, 72.00, 72.00, '2026-07-23T16:00:00Z'),
                (9104, 9104, 9105, @employeeId, 9101, '2026-07-25', 1, 117.00, 99.45, '2026-07-23T17:00:00Z'),
                (9105, 9105, 9104, @employeeId, NULL, '2026-07-23', 3, 93.00, 93.00, '2026-07-21T18:00:00Z'),
                (9106, 9101, 9106, @employeeId, NULL, '2026-07-26', 2, 117.00, 117.00, '2026-07-23T19:00:00Z');
            SET IDENTITY_INSERT [Reservations] OFF;

            SET IDENTITY_INSERT [Payments] ON;
            INSERT INTO [Payments] ([Id], [ReservationId], [PaymentMethodId], [Amount], [PaymentDate], [Note])
            VALUES
                (9101, 9101, 1, 61.20, '2026-07-24T14:35:00Z', N'Paid in full at the front desk.'),
                (9102, 9102, 3, 40.00, '2026-07-23T17:20:00Z', N'Credit card deposit.'),
                (9103, 9106, 5, 117.00, '2026-07-23T19:15:00Z', N'Paid in full via Venmo.');
            SET IDENTITY_INSERT [Payments] OFF;

            SET IDENTITY_INSERT [ReservationAudits] ON;
            INSERT INTO [ReservationAudits] ([Id], [ReservationId], [Action], [Description], [Username], [CreatedAt])
            VALUES
                (9101, 9101, N'Created', N'Reservation created for Maya Johnson at Hudson Court.', N'admin', '2026-07-22T14:00:00Z'),
                (9102, 9101, N'StatusChanged', N'Status changed from Confirmed to Completed.', N'admin', '2026-07-24T14:30:00Z'),
                (9103, 9102, N'Created', N'Reservation created for Ethan Brooks at Brooklyn Indoor.', N'admin', '2026-07-22T15:00:00Z'),
                (9104, 9103, N'Created', N'Reservation created for Sofia Martinez at Hudson Court.', N'admin', '2026-07-23T16:00:00Z'),
                (9105, 9104, N'Created', N'Reservation created for Liam Carter at Manhattan Premium.', N'admin', '2026-07-23T17:00:00Z'),
                (9106, 9105, N'Created', N'Reservation created for Ava Thompson at Brooklyn Indoor.', N'admin', '2026-07-21T18:00:00Z'),
                (9107, 9105, N'StatusChanged', N'Status changed from Pending to Canceled.', N'admin', '2026-07-22T18:30:00Z'),
                (9108, 9106, N'Created', N'Reservation created for Maya Johnson at Manhattan Premium.', N'admin', '2026-07-23T19:00:00Z');
            SET IDENTITY_INSERT [ReservationAudits] OFF;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM [ReservationAudits] WHERE [Id] BETWEEN 9101 AND 9108;
            DELETE FROM [Payments] WHERE [Id] BETWEEN 9101 AND 9103;
            DELETE FROM [Reservations] WHERE [Id] BETWEEN 9101 AND 9106;
            DELETE FROM [AvailableTurns] WHERE [Id] BETWEEN 9101 AND 9106;
            DELETE FROM [Promotions] WHERE [Id] BETWEEN 9101 AND 9102;
            DELETE FROM [Clients] WHERE [Id] BETWEEN 9101 AND 9105;
            DELETE FROM [Courts] WHERE [Id] BETWEEN 9101 AND 9103;
            DELETE FROM [People] WHERE [Id] BETWEEN 9101 AND 9105;
            """);
    }
}
