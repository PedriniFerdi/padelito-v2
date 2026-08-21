SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF (SELECT COUNT(*) FROM dbo.Roles WITH (UPDLOCK, HOLDLOCK) WHERE Id IN (1, 2, 3)) <> 3
        OR (SELECT COUNT(*) FROM dbo.ReservationStatuses WITH (UPDLOCK, HOLDLOCK) WHERE Id IN (1, 2, 3, 4)) <> 4
        OR (SELECT COUNT(*) FROM dbo.PaymentMethods WITH (UPDLOCK, HOLDLOCK) WHERE Id IN (1, 2, 3, 4, 5)) <> 5
        OR (SELECT COUNT(*) FROM dbo.CourtTypes WITH (UPDLOCK, HOLDLOCK) WHERE Id IN (1, 2, 3, 4)) <> 4
        THROW 51108, 'The expected global reference catalogs are incomplete.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Clubs WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = 1
          AND
          (
              (Name = N'Padelito' AND Email = N'admin@padelito.com')
              OR
              (Name = N'Padelito Demo Club' AND Email = N'hello@padelito-demo.com')
          )
    )
        THROW 51110, 'Club 1 is not the approved Spanish/English demo club.', 1;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Employees WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = 1 AND PersonId = 1 AND ClubId = 1
    )
        THROW 51111, 'Employee 1 does not link demo Person 1 to Club 1.', 1;

    IF (SELECT COUNT(*) FROM dbo.People WITH (UPDLOCK, HOLDLOCK) WHERE Id BETWEEN 9001 AND 9004) <> 4
        THROW 51112, 'The four expected demo People rows are not present.', 1;

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
        THROW 51113, 'A demo Person ID is occupied by an unapproved identity.', 1;

    IF (SELECT COUNT(*) FROM dbo.Clients WITH (UPDLOCK, HOLDLOCK) WHERE Id BETWEEN 9001 AND 9004 AND PersonId = Id) <> 4
        THROW 51114, 'The expected demo Client-to-Person mapping is not present.', 1;

    IF (SELECT COUNT(*) FROM dbo.Courts WITH (UPDLOCK, HOLDLOCK) WHERE Id BETWEEN 9001 AND 9003 AND ClubId = 1) <> 3
        THROW 51115, 'The three expected demo Courts are not present in Club 1.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Courts
        WHERE Id BETWEEN 9001 AND 9003
          AND NOT
          (
              (Id = 9001 AND Name IN (N'Central Demo', N'Center Court'))
              OR (Id = 9002 AND Name IN (N'Norte Demo', N'Grandstand'))
              OR (Id = 9003 AND Name IN (N'Arena Demo', N'The Arena'))
          )
    )
        THROW 51116, 'A demo Court ID is occupied by an unapproved court.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.Courts WITH (UPDLOCK, HOLDLOCK)
        WHERE Id = 9004 AND (ClubId <> 1 OR Name <> N'Match Point')
    )
        THROW 51117, 'Court 9004 is occupied by non-demo data.', 1;

    IF (SELECT COUNT(*) FROM dbo.Promotions WITH (UPDLOCK, HOLDLOCK) WHERE Id = 9001) <> 1
        THROW 51118, 'Demo Promotion 9001 is not present.', 1;

    IF (SELECT COUNT(*) FROM dbo.AvailableTurns WITH (UPDLOCK, HOLDLOCK) WHERE Id BETWEEN 9001 AND 9005) <> 5
        THROW 51119, 'The five expected legacy demo time slots are not present.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.AvailableTurns WITH (UPDLOCK, HOLDLOCK)
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
        THROW 51120, 'Time slot 9007 or 9008 is occupied by non-demo data.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.Reservations WITH (UPDLOCK, HOLDLOCK)
        WHERE Id BETWEEN 9001 AND 9005
          AND EmployeeId = 1
          AND ClientId BETWEEN 9001 AND 9004
          AND AvailableTurnId BETWEEN 9001 AND 9005
    ) <> 5
        THROW 51121, 'The expected legacy demo Reservation relationships are not present.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.Payments WITH (UPDLOCK, HOLDLOCK)
        WHERE Id BETWEEN 9001 AND 9003
          AND ReservationId BETWEEN 9001 AND 9003
    ) <> 3
        THROW 51122, 'The expected legacy demo Payment relationships are not present.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.ReservationAudits WITH (UPDLOCK, HOLDLOCK)
        WHERE Id BETWEEN 9001 AND 9007
          AND ReservationId BETWEEN 9001 AND 9005
    ) <> 7
        THROW 51123, 'The expected legacy demo ReservationAudit relationships are not present.', 1;

    UPDATE dbo.Roles
    SET Name = CASE Id
        WHEN 1 THEN N'Admin'
        WHEN 2 THEN N'Reception'
        WHEN 3 THEN N'Staff'
    END
    WHERE Id IN (1, 2, 3);

    UPDATE dbo.ReservationStatuses
    SET Name = CASE Id
        WHEN 1 THEN N'Pending'
        WHEN 2 THEN N'Confirmed'
        WHEN 3 THEN N'Canceled'
        WHEN 4 THEN N'Completed'
    END
    WHERE Id IN (1, 2, 3, 4);

    UPDATE dbo.PaymentMethods
    SET Description = CASE Id
        WHEN 1 THEN N'Cash'
        WHEN 2 THEN N'Debit Card'
        WHEN 3 THEN N'Credit Card'
        WHEN 4 THEN N'Bank Transfer'
        WHEN 5 THEN N'Venmo'
    END
    WHERE Id IN (1, 2, 3, 4, 5);

    UPDATE dbo.CourtTypes
    SET Description = CASE Id
        WHEN 1 THEN N'Concrete'
        WHEN 2 THEN N'Synthetic Turf'
        WHEN 3 THEN N'Indoor'
        WHEN 4 THEN N'Premium'
    END
    WHERE Id IN (1, 2, 3, 4);

    UPDATE dbo.Clubs
    SET Name = N'Padelito Demo Club',
        Address = N'120 Hudson St, New York, NY',
        Phone = N'(212) 555-0100',
        Email = N'hello@padelito-demo.com'
    WHERE Id = 1;

    UPDATE dbo.People
    SET FirstName = N'Alex',
        LastName = N'Morgan',
        Dni = N'555010100',
        Phone = N'(212) 555-0100',
        Email = N'alex.morgan@padelito-demo.com'
    WHERE Id = 1;

    UPDATE dbo.People
    SET FirstName = CASE Id
            WHEN 9001 THEN N'Maya'
            WHEN 9002 THEN N'Ethan'
            WHEN 9003 THEN N'Sofia'
            WHEN 9004 THEN N'Liam'
        END,
        LastName = CASE Id
            WHEN 9001 THEN N'Johnson'
            WHEN 9002 THEN N'Brooks'
            WHEN 9003 THEN N'Martinez'
            WHEN 9004 THEN N'Carter'
        END,
        Dni = CASE Id
            WHEN 9001 THEN N'555010101'
            WHEN 9002 THEN N'555010102'
            WHEN 9003 THEN N'555010103'
            WHEN 9004 THEN N'555010104'
        END,
        Phone = CASE Id
            WHEN 9001 THEN N'(212) 555-0101'
            WHEN 9002 THEN N'(212) 555-0102'
            WHEN 9003 THEN N'(718) 555-0103'
            WHEN 9004 THEN N'(646) 555-0104'
        END,
        Email = CASE Id
            WHEN 9001 THEN N'maya.johnson@example.com'
            WHEN 9002 THEN N'ethan.brooks@example.com'
            WHEN 9003 THEN N'sofia.martinez@example.com'
            WHEN 9004 THEN N'liam.carter@example.com'
        END
    WHERE Id BETWEEN 9001 AND 9004;

    UPDATE dbo.Promotions
    SET Name = N'Weekday Off-Peak',
        Description = N'15% off selected weekday reservations.',
        DiscountPercentage = 15.00
    WHERE Id = 9001;

    UPDATE dbo.Courts
    SET Name = CASE Id
            WHEN 9001 THEN N'Center Court'
            WHEN 9002 THEN N'Grandstand'
            WHEN 9003 THEN N'The Arena'
        END,
        HourPrice = CASE Id
            WHEN 9001 THEN 25.00
            WHEN 9002 THEN 27.00
            WHEN 9003 THEN 30.00
        END
    WHERE Id BETWEEN 9001 AND 9003;

    IF NOT EXISTS (SELECT 1 FROM dbo.Courts WHERE Id = 9004)
    BEGIN
        SET IDENTITY_INSERT dbo.Courts ON;
        INSERT INTO dbo.Courts (Id, ClubId, CourtTypeId, Name, HourPrice, IsActive)
        VALUES (9004, 1, 2, N'Match Point', 28.00, 1);
        SET IDENTITY_INSERT dbo.Courts OFF;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.AvailableTurns WHERE Id = 9007)
    BEGIN
        SET IDENTITY_INSERT dbo.AvailableTurns ON;
        INSERT INTO dbo.AvailableTurns (Id, CourtId, StartTime, EndTime, IsActive)
        VALUES (9007, 9004, '12:00:00', '13:30:00', 1);
        SET IDENTITY_INSERT dbo.AvailableTurns OFF;
    END;

    IF NOT EXISTS (SELECT 1 FROM dbo.AvailableTurns WHERE Id = 9008)
    BEGIN
        SET IDENTITY_INSERT dbo.AvailableTurns ON;
        INSERT INTO dbo.AvailableTurns (Id, CourtId, StartTime, EndTime, IsActive)
        VALUES (9008, 9004, '21:00:00', '22:30:00', 1);
        SET IDENTITY_INSERT dbo.AvailableTurns OFF;
    END;

    UPDATE dbo.Reservations
    SET BasePrice = CASE Id
            WHEN 9001 THEN 37.50
            WHEN 9002 THEN 40.50
            WHEN 9003 THEN 37.50
            WHEN 9004 THEN 40.50
            WHEN 9005 THEN 45.00
        END,
        FinalPrice = CASE Id
            WHEN 9001 THEN 31.88
            WHEN 9002 THEN 40.50
            WHEN 9003 THEN 37.50
            WHEN 9004 THEN 34.43
            WHEN 9005 THEN 45.00
        END
    WHERE Id BETWEEN 9001 AND 9005;

    UPDATE dbo.Payments
    SET Amount = CASE Id
            WHEN 9001 THEN 31.88
            WHEN 9002 THEN 40.50
            WHEN 9003 THEN 37.50
        END,
        Note = CASE Id
            WHEN 9001 THEN N'Paid in full via bank transfer.'
            WHEN 9002 THEN N'Paid in full with cash.'
            WHEN 9003 THEN N'Paid in full via Venmo.'
        END
    WHERE Id BETWEEN 9001 AND 9003;

    UPDATE dbo.ReservationAudits
    SET Action = CASE Action
            WHEN N'Creacion' THEN N'Created'
            WHEN N'CambioEstado' THEN N'StatusChanged'
            ELSE Action
        END,
        Description = CASE Id
            WHEN 9001 THEN N'Reservation created with status Completed for Maya Johnson, court Center Court, time slot 10:00-11:30.'
            WHEN 9002 THEN N'Status changed from Confirmed to Completed.'
            WHEN 9003 THEN N'Reservation created with status Completed for Ethan Brooks, court Grandstand, time slot 16:00-17:30.'
            WHEN 9004 THEN N'Reservation created with status Confirmed for Sofia Martinez, court Center Court, time slot 18:00-19:30.'
            WHEN 9005 THEN N'Reservation created with status Pending for Liam Carter, court Grandstand, time slot 20:00-21:30.'
            WHEN 9006 THEN N'Reservation created with status Canceled for Maya Johnson, court The Arena, time slot 19:00-20:30.'
            WHEN 9007 THEN N'Status changed from Pending to Canceled.'
        END
    WHERE Id BETWEEN 9001 AND 9007;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.Clubs
        WHERE Id = 1
          AND Name = N'Padelito Demo Club'
          AND Address = N'120 Hudson St, New York, NY'
          AND Email = N'hello@padelito-demo.com'
    )
        THROW 51130, 'English demo club postcondition failed.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.Roles
        WHERE (Id = 1 AND Name = N'Admin')
           OR (Id = 2 AND Name = N'Reception')
           OR (Id = 3 AND Name = N'Staff')
    ) <> 3
        OR
        (
            SELECT COUNT(*)
            FROM dbo.ReservationStatuses
            WHERE (Id = 1 AND Name = N'Pending')
               OR (Id = 2 AND Name = N'Confirmed')
               OR (Id = 3 AND Name = N'Canceled')
               OR (Id = 4 AND Name = N'Completed')
        ) <> 4
        OR
        (
            SELECT COUNT(*)
            FROM dbo.PaymentMethods
            WHERE (Id = 1 AND Description = N'Cash')
               OR (Id = 2 AND Description = N'Debit Card')
               OR (Id = 3 AND Description = N'Credit Card')
               OR (Id = 4 AND Description = N'Bank Transfer')
               OR (Id = 5 AND Description = N'Venmo')
        ) <> 5
        OR
        (
            SELECT COUNT(*)
            FROM dbo.CourtTypes
            WHERE (Id = 1 AND Description = N'Concrete')
               OR (Id = 2 AND Description = N'Synthetic Turf')
               OR (Id = 3 AND Description = N'Indoor')
               OR (Id = 4 AND Description = N'Premium')
        ) <> 4
        THROW 51129, 'English reference-catalog postcondition failed.', 1;

    IF
    (
        SELECT COUNT(*)
        FROM dbo.People
        WHERE (Id = 9001 AND Email = N'maya.johnson@example.com')
           OR (Id = 9002 AND Email = N'ethan.brooks@example.com')
           OR (Id = 9003 AND Email = N'sofia.martinez@example.com')
           OR (Id = 9004 AND Email = N'liam.carter@example.com')
    ) <> 4
        THROW 51131, 'English demo people postcondition failed.', 1;

    IF (SELECT COUNT(*) FROM dbo.Courts WHERE Id BETWEEN 9001 AND 9004 AND ClubId = 1) <> 4
        THROW 51132, 'English demo courts postcondition failed.', 1;

    IF (SELECT COUNT(*) FROM dbo.AvailableTurns WHERE Id BETWEEN 9007 AND 9008 AND CourtId = 9004) <> 2
        THROW 51133, 'English demo time-slot postcondition failed.', 1;

    IF (SELECT COUNT(*) FROM dbo.Payments WHERE Id BETWEEN 9001 AND 9003 AND Amount IN (31.88, 40.50, 37.50)) <> 3
        THROW 51134, 'English demo payment postcondition failed.', 1;

    COMMIT TRANSACTION;

    SELECT
        N'UPDATED' AS [Status],
        DB_NAME() AS [DatabaseName],
        (SELECT COUNT(*) FROM dbo.People WHERE Id BETWEEN 9001 AND 9004) AS [DemoPeople],
        (SELECT COUNT(*) FROM dbo.Courts WHERE Id BETWEEN 9001 AND 9004) AS [DemoCourts],
        (SELECT COUNT(*) FROM dbo.Reservations WHERE Id BETWEEN 9001 AND 9005) AS [DemoReservations],
        (SELECT COUNT(*) FROM dbo.Payments WHERE Id BETWEEN 9001 AND 9003) AS [DemoPayments],
        (SELECT COALESCE(SUM(Amount), 0) FROM dbo.Payments WHERE Id BETWEEN 9001 AND 9003) AS [DemoPaymentTotal];
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
