/*
PRD-BLK-003 read-only preflight.

Run this script before EnforceFullReservationPayment. It reports payment-history
compatibility but never decides how an exception should be reconciled. Do not
edit or delete financial rows merely to make the migration pass.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'[dbo].[Payments]', N'U') IS NULL
    OR OBJECT_ID(N'[dbo].[Reservations]', N'U') IS NULL
BEGIN
    ;THROW 51003, 'PRD-BLK-003 preflight requires the Payments and Reservations tables.', 1;
END;

IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]', N'U') IS NULL
BEGIN
    SELECT
        N'20260724201719_EnforceFullReservationPayment' AS [MigrationId],
        CAST(0 AS bit) AS [IsApplied],
        N'__EFMigrationsHistory does not exist.' AS [Evidence];
END
ELSE
BEGIN
    SELECT
        N'20260724201719_EnforceFullReservationPayment' AS [MigrationId],
        CONVERT(
            bit,
            CASE WHEN EXISTS (
                SELECT 1
                FROM [dbo].[__EFMigrationsHistory]
                WHERE [MigrationId] = N'20260724201719_EnforceFullReservationPayment'
            ) THEN 1 ELSE 0 END) AS [IsApplied],
        N'Read from __EFMigrationsHistory.' AS [Evidence];
END;

SELECT
    COUNT_BIG(*) AS [TotalPaymentRows],
    COALESCE(
        SUM(CONVERT(decimal(38, 2), payment.[Amount])),
        CONVERT(decimal(38, 2), 0)) AS [TotalPaymentAmount]
FROM [dbo].[Payments] AS payment;

WITH [PaymentAssessment] AS
(
    SELECT
        reservation.[Id] AS [ReservationId],
        reservation.[FinalPrice],
        COUNT_BIG(payment.[Id]) AS [PaymentCount],
        COALESCE(
            SUM(CONVERT(decimal(38, 2), payment.[Amount])),
            CONVERT(decimal(38, 2), 0)) AS [PaymentTotal]
    FROM [dbo].[Reservations] AS reservation
    LEFT JOIN [dbo].[Payments] AS payment
        ON payment.[ReservationId] = reservation.[Id]
    GROUP BY reservation.[Id], reservation.[FinalPrice]
),
[Classified] AS
(
    SELECT
        [ReservationId],
        [FinalPrice],
        [PaymentCount],
        [PaymentTotal],
        CASE
            WHEN [PaymentCount] = 0 THEN N'ZERO_PAYMENTS'
            WHEN [PaymentCount] = 1 AND [PaymentTotal] = [FinalPrice]
                THEN N'ONE_EXACT_PAYMENT'
            WHEN [PaymentCount] = 1 THEN N'ONE_NON_MATCHING_PAYMENT'
            ELSE N'MULTIPLE_PAYMENTS'
        END AS [CompatibilityClass]
    FROM [PaymentAssessment]
)
SELECT
    [CompatibilityClass],
    COUNT_BIG(*) AS [ReservationCount],
    SUM([PaymentCount]) AS [PaymentRows],
    SUM([PaymentTotal]) AS [PaymentTotal]
FROM [Classified]
GROUP BY [CompatibilityClass]
ORDER BY [CompatibilityClass];

WITH [PaymentAssessment] AS
(
    SELECT
        reservation.[Id] AS [ReservationId],
        reservation.[FinalPrice],
        COUNT_BIG(payment.[Id]) AS [PaymentCount],
        COALESCE(
            SUM(CONVERT(decimal(38, 2), payment.[Amount])),
            CONVERT(decimal(38, 2), 0)) AS [PaymentTotal]
    FROM [dbo].[Reservations] AS reservation
    LEFT JOIN [dbo].[Payments] AS payment
        ON payment.[ReservationId] = reservation.[Id]
    GROUP BY reservation.[Id], reservation.[FinalPrice]
)
SELECT
    court.[ClubId],
    assessment.[ReservationId],
    assessment.[PaymentCount],
    assessment.[FinalPrice],
    assessment.[PaymentTotal],
    assessment.[PaymentTotal] - assessment.[FinalPrice] AS [Variance],
    CASE
        WHEN assessment.[PaymentCount] = 1
            THEN N'ONE_NON_MATCHING_PAYMENT'
        ELSE N'MULTIPLE_PAYMENTS'
    END AS [ExceptionClass]
FROM [PaymentAssessment] AS assessment
INNER JOIN [dbo].[Reservations] AS reservation
    ON reservation.[Id] = assessment.[ReservationId]
INNER JOIN [dbo].[AvailableTurns] AS availableTurn
    ON availableTurn.[Id] = reservation.[AvailableTurnId]
INNER JOIN [dbo].[Courts] AS court
    ON court.[Id] = availableTurn.[CourtId]
WHERE assessment.[PaymentCount] > 1
    OR (
        assessment.[PaymentCount] = 1
        AND assessment.[PaymentTotal]
            <> CONVERT(decimal(38, 2), assessment.[FinalPrice])
    )
ORDER BY court.[ClubId], assessment.[ReservationId];

WITH [IncompatibleReservations] AS
(
    SELECT payment.[ReservationId]
    FROM [dbo].[Payments] AS payment
    INNER JOIN [dbo].[Reservations] AS reservation
        ON reservation.[Id] = payment.[ReservationId]
    GROUP BY payment.[ReservationId], reservation.[FinalPrice]
    HAVING COUNT_BIG(*) <> 1
        OR SUM(CONVERT(decimal(38, 2), payment.[Amount]))
            <> CONVERT(decimal(38, 2), reservation.[FinalPrice])
)
SELECT
    court.[ClubId],
    payment.[ReservationId],
    payment.[Id] AS [PaymentId],
    payment.[PaymentMethodId],
    payment.[PaymentDate],
    payment.[Amount],
    payment.[Note]
FROM [dbo].[Payments] AS payment
INNER JOIN [IncompatibleReservations] AS incompatible
    ON incompatible.[ReservationId] = payment.[ReservationId]
INNER JOIN [dbo].[Reservations] AS reservation
    ON reservation.[Id] = payment.[ReservationId]
INNER JOIN [dbo].[AvailableTurns] AS availableTurn
    ON availableTurn.[Id] = reservation.[AvailableTurnId]
INNER JOIN [dbo].[Courts] AS court
    ON court.[Id] = availableTurn.[CourtId]
ORDER BY court.[ClubId], payment.[ReservationId], payment.[PaymentDate], payment.[Id];
