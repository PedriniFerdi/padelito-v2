SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

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
WHERE Id = 1
  AND EXISTS (SELECT 1 FROM dbo.Employees WHERE PersonId = 1);

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
    SELECT 9004, ClubId, 2, N'Match Point', 28.00, 1
    FROM dbo.Courts
    WHERE Id = 9001;
    SET IDENTITY_INSERT dbo.Courts OFF;
END
ELSE
BEGIN
    UPDATE dbo.Courts
    SET Name = N'Match Point',
        HourPrice = 28.00,
        CourtTypeId = 2,
        IsActive = 1
    WHERE Id = 9004;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AvailableTurns WHERE Id = 9006)
BEGIN
    SET IDENTITY_INSERT dbo.AvailableTurns ON;
    INSERT INTO dbo.AvailableTurns (Id, CourtId, StartTime, EndTime, IsActive)
    VALUES (9006, 9004, '12:00:00', '13:30:00', 1);
    SET IDENTITY_INSERT dbo.AvailableTurns OFF;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.AvailableTurns WHERE Id = 9007)
BEGIN
    SET IDENTITY_INSERT dbo.AvailableTurns ON;
    INSERT INTO dbo.AvailableTurns (Id, CourtId, StartTime, EndTime, IsActive)
    VALUES (9007, 9004, '21:00:00', '22:30:00', 1);
    SET IDENTITY_INSERT dbo.AvailableTurns OFF;
END;

UPDATE dbo.Reservations
SET BasePrice = CASE Id
        WHEN 9001 THEN 37.50
        WHEN 9002 THEN 40.50
        WHEN 9003 THEN 37.50
        WHEN 9004 THEN 40.50
        WHEN 9005 THEN 45.00
        WHEN 9006 THEN 40.50
    END,
    FinalPrice = CASE Id
        WHEN 9001 THEN 31.88
        WHEN 9002 THEN 40.50
        WHEN 9003 THEN 37.50
        WHEN 9004 THEN 34.43
        WHEN 9005 THEN 45.00
        WHEN 9006 THEN 40.50
    END
WHERE Id BETWEEN 9001 AND 9006;

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
END;

UPDATE dbo.ReservationAudits
SET Description = CASE Id
    WHEN 9001 THEN N'Reservation created with status Completed for Maya Johnson, court Center Court, time slot 10:00-11:30.'
    WHEN 9002 THEN N'Status changed from Confirmed to Completed.'
    WHEN 9003 THEN N'Reservation created with status Completed for Ethan Brooks, court Grandstand, time slot 16:00-17:30.'
    WHEN 9004 THEN N'Reservation created with status Confirmed for Sofia Martinez, court Center Court, time slot 18:00-19:30.'
    WHEN 9005 THEN N'Reservation created with status Pending for Liam Carter, court Grandstand, time slot 20:00-21:30.'
    WHEN 9006 THEN N'Reservation created with status Canceled for Maya Johnson, court The Arena, time slot 19:00-20:30.'
    WHEN 9007 THEN N'Status changed from Pending to Canceled.'
    WHEN 9008 THEN N'Status changed from Pending to Canceled.'
    WHEN 9009 THEN N'Reservation created with status Confirmed for Ferdinando Perez, court Grandstand, time slot 20:00-21:30.'
    ELSE Description
END
WHERE Id BETWEEN 9001 AND 9009;

COMMIT TRANSACTION;
