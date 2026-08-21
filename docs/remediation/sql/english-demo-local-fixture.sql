SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() NOT LIKE N'PadelitoEnglishDemoTest[_]%'
    THROW 51190, 'This legacy fixture may run only in a PadelitoEnglishDemoTest_* disposable database.', 1;

IF EXISTS (SELECT 1 FROM dbo.Clubs)
    THROW 51191, 'The disposable fixture database must not contain application-owned data.', 1;

BEGIN TRANSACTION;

SET IDENTITY_INSERT dbo.Clubs ON;
INSERT INTO dbo.Clubs (Id, Name, Address, Phone, Email, IsActive, CreatedAt)
VALUES (1, N'Padelito', N'Buenos Aires', N'11-4000-0000', N'admin@padelito.com', 1, '2026-07-08T00:00:00');
SET IDENTITY_INSERT dbo.Clubs OFF;

SET IDENTITY_INSERT dbo.People ON;
INSERT INTO dbo.People (Id, FirstName, LastName, Dni, Phone, Email, IsActive, CreatedAt)
VALUES
    (1, N'Carlos', N'Benitez', N'30111222', N'11-4000-1001', N'carlos.benitez@padelito.com', 1, '2026-07-08T00:00:00'),
    (9001, N'Lucía', N'Fernández', N'35421678', N'11-5821-4076', N'lucia.fernandez@example.com', 1, '2026-07-08T00:00:00'),
    (9002, N'Martín', N'Sosa', N'32987412', N'11-4962-1180', N'martin.sosa@example.com', 1, '2026-07-08T00:00:00'),
    (9003, N'Valentina', N'Ríos', N'38741209', N'11-6234-9041', N'valentina.rios@example.com', 1, '2026-07-08T00:00:00'),
    (9004, N'Nicolás', N'Acosta', N'36109874', N'11-4487-6620', N'nicolas.acosta@example.com', 1, '2026-07-08T00:00:00');
SET IDENTITY_INSERT dbo.People OFF;

SET IDENTITY_INSERT dbo.Employees ON;
INSERT INTO dbo.Employees (Id, PersonId, ClubId) VALUES (1, 1, 1);
SET IDENTITY_INSERT dbo.Employees OFF;

SET IDENTITY_INSERT dbo.Courts ON;
INSERT INTO dbo.Courts (Id, ClubId, CourtTypeId, Name, HourPrice, IsActive)
VALUES
    (9001, 1, 2, N'Central Demo', 18000.00, 1),
    (9002, 1, 3, N'Norte Demo', 22000.00, 1),
    (9003, 1, 4, N'Arena Demo', 26000.00, 1);
SET IDENTITY_INSERT dbo.Courts OFF;

SET IDENTITY_INSERT dbo.Clients ON;
INSERT INTO dbo.Clients (Id, PersonId)
VALUES (9001, 9001), (9002, 9002), (9003, 9003), (9004, 9004);
SET IDENTITY_INSERT dbo.Clients OFF;

SET IDENTITY_INSERT dbo.Promotions ON;
INSERT INTO dbo.Promotions (Id, Name, Description, DiscountPercentage, DateFrom, DateTo, IsActive)
VALUES (9001, N'Horario tranquilo demo', N'Beneficio demo para turnos seleccionados.', 15.00, '2026-07-01', '2026-12-31', 1);
SET IDENTITY_INSERT dbo.Promotions OFF;

SET IDENTITY_INSERT dbo.AvailableTurns ON;
INSERT INTO dbo.AvailableTurns (Id, CourtId, StartTime, EndTime, IsActive)
VALUES
    (9001, 9001, '10:00:00', '11:30:00', 1),
    (9002, 9001, '18:00:00', '19:30:00', 1),
    (9003, 9002, '16:00:00', '17:30:00', 1),
    (9004, 9002, '20:00:00', '21:30:00', 1),
    (9005, 9003, '19:00:00', '20:30:00', 1);
SET IDENTITY_INSERT dbo.AvailableTurns OFF;

SET IDENTITY_INSERT dbo.Reservations ON;
INSERT INTO dbo.Reservations
    (Id, ClientId, AvailableTurnId, EmployeeId, PromotionId, ReservationDate,
     ReservationStatusId, BasePrice, FinalPrice, CreatedAt)
VALUES
    (9001, 9001, 9001, 1, 9001, '2026-07-08', 4, 27000.00, 22950.00, '2026-07-08T10:00:00'),
    (9002, 9002, 9003, 1, NULL, '2026-07-09', 4, 33000.00, 33000.00, '2026-07-08T12:00:00'),
    (9003, 9003, 9002, 1, NULL, '2026-07-12', 2, 27000.00, 27000.00, '2026-07-10T14:00:00'),
    (9004, 9004, 9004, 1, 9001, '2026-07-13', 1, 33000.00, 28050.00, '2026-07-11T16:00:00'),
    (9005, 9001, 9005, 1, NULL, '2026-07-10', 3, 39000.00, 39000.00, '2026-07-09T09:00:00');
SET IDENTITY_INSERT dbo.Reservations OFF;

SET IDENTITY_INSERT dbo.Payments ON;
INSERT INTO dbo.Payments (Id, ReservationId, PaymentMethodId, Amount, PaymentDate, Note)
VALUES
    (9001, 9001, 4, 22950.00, '2026-07-09T11:00:00', N'Payment completo demo'),
    (9002, 9002, 1, 15000.00, '2026-07-09T13:00:00', N'Seña en efectivo'),
    (9003, 9003, 5, 27000.00, '2026-07-11T15:00:00', N'Venmo');
SET IDENTITY_INSERT dbo.Payments OFF;

SET IDENTITY_INSERT dbo.ReservationAudits ON;
INSERT INTO dbo.ReservationAudits (Id, ReservationId, Action, Description, Username, CreatedAt)
VALUES
    (9001, 9001, N'Creacion', N'Reserva demo creada para Lucía Fernández en Central Demo.', N'admin', '2026-07-08T10:00:00'),
    (9002, 9001, N'CambioEstado', N'Estado cambiado de Confirmada a Finalizada.', N'admin', '2026-07-09T12:00:00'),
    (9003, 9002, N'Creacion', N'Reserva demo creada para Martín Sosa en Norte Demo.', N'admin', '2026-07-08T12:00:00'),
    (9004, 9003, N'Creacion', N'Reserva demo creada para Valentina Ríos en Central Demo.', N'admin', '2026-07-10T14:00:00'),
    (9005, 9004, N'Creacion', N'Reserva demo creada para Nicolás Acosta en Norte Demo.', N'admin', '2026-07-11T16:00:00'),
    (9006, 9005, N'Creacion', N'Reserva demo creada para Lucía Fernández en Arena Demo.', N'admin', '2026-07-09T09:00:00'),
    (9007, 9005, N'CambioEstado', N'Estado cambiado de Pendiente a Cancelada.', N'admin', '2026-07-09T10:00:00');
SET IDENTITY_INSERT dbo.ReservationAudits OFF;

COMMIT TRANSACTION;
