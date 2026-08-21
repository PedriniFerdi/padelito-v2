SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Issues TABLE
(
    [Reason] nvarchar(4000) NOT NULL
);

IF
(
    SELECT COUNT(*)
    FROM dbo.Roles
    WHERE (Id = 1 AND Name = N'Admin')
       OR (Id = 2 AND Name = N'Reception')
       OR (Id = 3 AND Name = N'Staff')
) <> 3
    INSERT INTO @Issues VALUES (N'English role catalog does not match.');

IF
(
    SELECT COUNT(*)
    FROM dbo.ReservationStatuses
    WHERE (Id = 1 AND Name = N'Pending')
       OR (Id = 2 AND Name = N'Confirmed')
       OR (Id = 3 AND Name = N'Canceled')
       OR (Id = 4 AND Name = N'Completed')
) <> 4
    INSERT INTO @Issues VALUES (N'English reservation-status catalog does not match.');

IF
(
    SELECT COUNT(*)
    FROM dbo.PaymentMethods
    WHERE (Id = 1 AND Description = N'Cash')
       OR (Id = 2 AND Description = N'Debit Card')
       OR (Id = 3 AND Description = N'Credit Card')
       OR (Id = 4 AND Description = N'Bank Transfer')
       OR (Id = 5 AND Description = N'Venmo')
) <> 5
    INSERT INTO @Issues VALUES (N'English payment-method catalog does not match.');

IF
(
    SELECT COUNT(*)
    FROM dbo.CourtTypes
    WHERE (Id = 1 AND Description = N'Concrete')
       OR (Id = 2 AND Description = N'Synthetic Turf')
       OR (Id = 3 AND Description = N'Indoor')
       OR (Id = 4 AND Description = N'Premium')
) <> 4
    INSERT INTO @Issues VALUES (N'English court-type catalog does not match.');

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Clubs
    WHERE Id = 1
      AND Name = N'Padelito Demo Club'
      AND Address = N'120 Hudson St, New York, NY'
      AND Phone = N'(212) 555-0100'
      AND Email = N'hello@padelito-demo.com'
)
    INSERT INTO @Issues VALUES (N'English demo club values do not match.');

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.People
    WHERE Id = 1
      AND FirstName = N'Alex'
      AND LastName = N'Morgan'
      AND Email = N'alex.morgan@padelito-demo.com'
)
    INSERT INTO @Issues VALUES (N'English demo administrator identity does not match.');

IF
(
    SELECT COUNT(*)
    FROM dbo.People
    WHERE (Id = 9001 AND FirstName = N'Maya' AND LastName = N'Johnson' AND Email = N'maya.johnson@example.com')
       OR (Id = 9002 AND FirstName = N'Ethan' AND LastName = N'Brooks' AND Email = N'ethan.brooks@example.com')
       OR (Id = 9003 AND FirstName = N'Sofia' AND LastName = N'Martinez' AND Email = N'sofia.martinez@example.com')
       OR (Id = 9004 AND FirstName = N'Liam' AND LastName = N'Carter' AND Email = N'liam.carter@example.com')
) <> 4
    INSERT INTO @Issues VALUES (N'English demo people values do not match.');

IF
(
    SELECT COUNT(*)
    FROM dbo.Courts
    WHERE (Id = 9001 AND ClubId = 1 AND Name = N'Center Court' AND HourPrice = 25.00)
       OR (Id = 9002 AND ClubId = 1 AND Name = N'Grandstand' AND HourPrice = 27.00)
       OR (Id = 9003 AND ClubId = 1 AND Name = N'The Arena' AND HourPrice = 30.00)
       OR (Id = 9004 AND ClubId = 1 AND Name = N'Match Point' AND HourPrice = 28.00)
) <> 4
    INSERT INTO @Issues VALUES (N'English demo court values do not match.');

IF
(
    SELECT COUNT(*)
    FROM dbo.AvailableTurns
    WHERE (Id = 9007 AND CourtId = 9004 AND StartTime = '12:00:00' AND EndTime = '13:30:00')
       OR (Id = 9008 AND CourtId = 9004 AND StartTime = '21:00:00' AND EndTime = '22:30:00')
) <> 2
    INSERT INTO @Issues VALUES (N'English demo Match Point time slots do not match.');

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Promotions
    WHERE Id = 9001
      AND Name = N'Weekday Off-Peak'
      AND DiscountPercentage = 15.00
)
    INSERT INTO @Issues VALUES (N'English demo promotion does not match.');

IF
(
    SELECT COUNT(*)
    FROM dbo.Payments
    WHERE (Id = 9001 AND ReservationId = 9001 AND Amount = 31.88)
       OR (Id = 9002 AND ReservationId = 9002 AND Amount = 40.50)
       OR (Id = 9003 AND ReservationId = 9003 AND Amount = 37.50)
) <> 3
    INSERT INTO @Issues VALUES (N'English demo payments do not match.');

IF EXISTS
(
    SELECT 1
    FROM dbo.ReservationAudits
    WHERE Id BETWEEN 9001 AND 9007
      AND
      (
          Action IN (N'Creacion', N'CambioEstado')
          OR Description LIKE N'%Lucía%'
          OR Description LIKE N'%Martín%'
          OR Description LIKE N'%Valentina%'
          OR Description LIKE N'%Nicolás%'
          OR Description LIKE N'%cambiado%'
          OR Description LIKE N'%creada%'
      )
)
    INSERT INTO @Issues VALUES (N'Spanish content remains in the approved demo audit range.');

IF EXISTS (SELECT 1 FROM @Issues)
BEGIN
    SELECT N'FAILED' AS [Status], [Reason]
    FROM @Issues
    ORDER BY [Reason];

    THROW 51140, 'English demo postflight failed.', 1;
END;

SELECT
    N'PASS' AS [Status],
    DB_NAME() AS [DatabaseName],
    (SELECT COUNT(*) FROM dbo.People WHERE Id BETWEEN 9001 AND 9004) AS [DemoPeople],
    (SELECT COUNT(*) FROM dbo.Courts WHERE Id BETWEEN 9001 AND 9004) AS [DemoCourts],
    (SELECT COUNT(*) FROM dbo.Reservations WHERE Id BETWEEN 9001 AND 9005) AS [DemoReservations],
    (SELECT COUNT(*) FROM dbo.Payments WHERE Id BETWEEN 9001 AND 9003) AS [DemoPayments],
    (SELECT COALESCE(SUM(Amount), 0) FROM dbo.Payments WHERE Id BETWEEN 9001 AND 9003) AS [DemoPaymentTotal];
