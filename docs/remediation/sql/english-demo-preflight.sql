SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @RequiredTables TABLE ([Name] sysname NOT NULL);
INSERT INTO @RequiredTables ([Name])
VALUES
    (N'__EFMigrationsHistory'),
    (N'Roles'),
    (N'ReservationStatuses'),
    (N'PaymentMethods'),
    (N'CourtTypes'),
    (N'Clubs'),
    (N'People'),
    (N'Employees'),
    (N'Clients'),
    (N'Courts'),
    (N'Promotions'),
    (N'AvailableTurns'),
    (N'Reservations'),
    (N'Payments'),
    (N'ReservationAudits');

IF EXISTS
(
    SELECT 1
    FROM @RequiredTables AS required
    WHERE OBJECT_ID(N'dbo.' + required.[Name], N'U') IS NULL
)
BEGIN
    SELECT
        N'BLOCKED' AS [Status],
        N'Missing required table: ' + required.[Name] AS [Reason]
    FROM @RequiredTables AS required
    WHERE OBJECT_ID(N'dbo.' + required.[Name], N'U') IS NULL;

    THROW 51100, 'English demo preflight failed because the schema is incomplete.', 1;
END;
GO

DECLARE @Issues TABLE
(
    [Reason] nvarchar(4000) NOT NULL
);

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.__EFMigrationsHistory
    WHERE MigrationId = N'20260712231612_CompleteDemoSeed'
)
    INSERT INTO @Issues VALUES (N'The historical CompleteDemoSeed migration is not recorded as applied.');

IF (SELECT COUNT(*) FROM dbo.Roles WHERE Id IN (1, 2, 3)) <> 3
    INSERT INTO @Issues VALUES (N'Expected reference Role IDs 1-3.');

IF (SELECT COUNT(*) FROM dbo.ReservationStatuses WHERE Id IN (1, 2, 3, 4)) <> 4
    INSERT INTO @Issues VALUES (N'Expected ReservationStatus IDs 1-4.');

IF (SELECT COUNT(*) FROM dbo.PaymentMethods WHERE Id IN (1, 2, 3, 4, 5)) <> 5
    INSERT INTO @Issues VALUES (N'Expected PaymentMethod IDs 1-5.');

IF (SELECT COUNT(*) FROM dbo.CourtTypes WHERE Id IN (1, 2, 3, 4)) <> 4
    INSERT INTO @Issues VALUES (N'Expected CourtType IDs 1-4.');

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Clubs
    WHERE Id = 1
      AND
      (
          (Name = N'Padelito' AND Email = N'admin@padelito.com')
          OR
          (Name = N'Padelito Demo Club' AND Email = N'hello@padelito-demo.com')
      )
)
    INSERT INTO @Issues VALUES (N'Club 1 is absent or is not the known Spanish/English demo club.');

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Employees
    WHERE Id = 1 AND PersonId = 1 AND ClubId = 1
)
    INSERT INTO @Issues VALUES (N'Employee 1 is absent or no longer links demo Person 1 to Club 1.');

IF (SELECT COUNT(*) FROM dbo.People WHERE Id BETWEEN 9001 AND 9004) <> 4
    INSERT INTO @Issues VALUES (N'Expected exactly four legacy demo people with IDs 9001-9004.');

IF EXISTS
(
    SELECT 1
    FROM dbo.People
    WHERE Id BETWEEN 9001 AND 9004
      AND NOT
      (
          (Id = 9001 AND Email IN (N'lucia.fernandez@example.com', N'maya.johnson@example.com'))
          OR (Id = 9002 AND Email IN (N'martin.sosa@example.com', N'ethan.brooks@example.com'))
          OR (Id = 9003 AND Email IN (N'valentina.rios@example.com', N'sofia.martinez@example.com'))
          OR (Id = 9004 AND Email IN (N'nicolas.acosta@example.com', N'liam.carter@example.com'))
      )
)
    INSERT INTO @Issues VALUES (N'A demo Person ID is occupied by an identity outside the approved Spanish/English mapping.');

IF EXISTS
(
    SELECT 1
    FROM dbo.Clients
    WHERE Id BETWEEN 9001 AND 9004
      AND PersonId <> Id
)
    OR (SELECT COUNT(*) FROM dbo.Clients WHERE Id BETWEEN 9001 AND 9004) <> 4
    INSERT INTO @Issues VALUES (N'Legacy demo Client IDs 9001-9004 do not map one-to-one to their People IDs.');

IF (SELECT COUNT(*) FROM dbo.Courts WHERE Id BETWEEN 9001 AND 9003) <> 3
    INSERT INTO @Issues VALUES (N'Expected the three legacy demo courts with IDs 9001-9003.');

IF EXISTS
(
    SELECT 1
    FROM dbo.Courts
    WHERE Id BETWEEN 9001 AND 9003
      AND
      (
          ClubId <> 1
          OR NOT
          (
              (Id = 9001 AND Name IN (N'Central Demo', N'Center Court'))
              OR (Id = 9002 AND Name IN (N'Norte Demo', N'Grandstand'))
              OR (Id = 9003 AND Name IN (N'Arena Demo', N'The Arena'))
          )
      )
)
    INSERT INTO @Issues VALUES (N'A demo Court ID is occupied by a court outside the approved Spanish/English mapping.');

IF EXISTS
(
    SELECT 1
    FROM dbo.Courts
    WHERE Id = 9004
      AND (ClubId <> 1 OR Name <> N'Match Point')
)
    INSERT INTO @Issues VALUES (N'Court 9004 already exists but is not the English demo Match Point court.');

IF (SELECT COUNT(*) FROM dbo.Promotions WHERE Id = 9001) <> 1
    INSERT INTO @Issues VALUES (N'Expected legacy demo Promotion 9001.');

IF (SELECT COUNT(*) FROM dbo.AvailableTurns WHERE Id BETWEEN 9001 AND 9005) <> 5
    INSERT INTO @Issues VALUES (N'Expected five legacy demo time slots with IDs 9001-9005.');

IF EXISTS
(
    SELECT 1
    FROM dbo.AvailableTurns
    WHERE Id IN (9007, 9008)
      AND NOT
      (
          CourtId = 9004
          AND
          (
              (Id = 9007 AND StartTime = '12:00:00' AND EndTime = '13:30:00')
              OR (Id = 9008 AND StartTime = '21:00:00' AND EndTime = '22:30:00')
          )
      )
)
    INSERT INTO @Issues VALUES (N'Time slot 9007 or 9008 is occupied by data outside the approved English demo mapping.');

IF (SELECT COUNT(*) FROM dbo.Reservations WHERE Id BETWEEN 9001 AND 9005) <> 5
    INSERT INTO @Issues VALUES (N'Expected five legacy demo Reservations with IDs 9001-9005.');

IF EXISTS
(
    SELECT 1
    FROM dbo.Reservations
    WHERE Id BETWEEN 9001 AND 9005
      AND
      (
          EmployeeId <> 1
          OR ClientId NOT BETWEEN 9001 AND 9004
          OR AvailableTurnId NOT BETWEEN 9001 AND 9005
      )
)
    INSERT INTO @Issues VALUES (N'Legacy demo reservation relationships no longer match the approved fixture.');

IF (SELECT COUNT(*) FROM dbo.Payments WHERE Id BETWEEN 9001 AND 9003) <> 3
    INSERT INTO @Issues VALUES (N'Expected three legacy demo Payments with IDs 9001-9003.');

IF EXISTS
(
    SELECT 1
    FROM dbo.Payments
    WHERE Id BETWEEN 9001 AND 9003
      AND ReservationId NOT BETWEEN 9001 AND 9003
)
    INSERT INTO @Issues VALUES (N'Legacy demo Payment relationships no longer match the approved fixture.');

IF (SELECT COUNT(*) FROM dbo.ReservationAudits WHERE Id BETWEEN 9001 AND 9007) <> 7
    INSERT INTO @Issues VALUES (N'Expected seven legacy demo ReservationAudits with IDs 9001-9007.');

IF EXISTS
(
    SELECT 1
    FROM dbo.ReservationAudits
    WHERE Id BETWEEN 9001 AND 9007
      AND ReservationId NOT BETWEEN 9001 AND 9005
)
    INSERT INTO @Issues VALUES (N'Legacy demo audit relationships no longer match the approved fixture.');

IF EXISTS (SELECT 1 FROM @Issues)
BEGIN
    SELECT N'BLOCKED' AS [Status], [Reason]
    FROM @Issues
    ORDER BY [Reason];

    THROW 51101, 'English demo preflight found data outside the approved mapping.', 1;
END;

SELECT
    N'READY' AS [Status],
    DB_NAME() AS [DatabaseName],
    (SELECT COUNT(*) FROM dbo.People WHERE Id BETWEEN 9001 AND 9004) AS [DemoPeople],
    (SELECT COUNT(*) FROM dbo.Courts WHERE Id BETWEEN 9001 AND 9004) AS [DemoCourts],
    (SELECT COUNT(*) FROM dbo.Reservations WHERE Id BETWEEN 9001 AND 9005) AS [DemoReservations],
    (SELECT COUNT(*) FROM dbo.Payments WHERE Id BETWEEN 9001 AND 9003) AS [DemoPayments],
    (SELECT COALESCE(SUM(Amount), 0) FROM dbo.Payments WHERE Id BETWEEN 9001 AND 9003) AS [DemoPaymentTotal];
