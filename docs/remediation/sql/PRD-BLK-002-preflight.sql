/*
PRD-BLK-002 read-only preflight.

This script reports evidence only. A fixed ID or matching literal is not proof
that a row is disposable demo data. Do not add UPDATE, DELETE, MERGE, TRUNCATE,
DROP, or other mutations to this file.
*/

SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;

SELECT [MigrationId], [ProductVersion]
FROM [dbo].[__EFMigrationsHistory]
ORDER BY [MigrationId];

SELECT N'Clubs' AS [TableName], COUNT_BIG(*) AS [RowCount] FROM [dbo].[Clubs]
UNION ALL SELECT N'People', COUNT_BIG(*) FROM [dbo].[People]
UNION ALL SELECT N'Clients', COUNT_BIG(*) FROM [dbo].[Clients]
UNION ALL SELECT N'Employees', COUNT_BIG(*) FROM [dbo].[Employees]
UNION ALL SELECT N'Users', COUNT_BIG(*) FROM [dbo].[Users]
UNION ALL SELECT N'Courts', COUNT_BIG(*) FROM [dbo].[Courts]
UNION ALL SELECT N'AvailableTurns', COUNT_BIG(*) FROM [dbo].[AvailableTurns]
UNION ALL SELECT N'Promotions', COUNT_BIG(*) FROM [dbo].[Promotions]
UNION ALL SELECT N'Reservations', COUNT_BIG(*) FROM [dbo].[Reservations]
UNION ALL SELECT N'Payments', COUNT_BIG(*) FROM [dbo].[Payments]
UNION ALL SELECT N'ReservationAudits', COUNT_BIG(*) FROM [dbo].[ReservationAudits]
ORDER BY [TableName];

SELECT
    COUNT_BIG(*) AS [ReservationCount],
    COALESCE(SUM([BasePrice]), 0) AS [TotalBasePrice],
    COALESCE(SUM([FinalPrice]), 0) AS [TotalFinalPrice]
FROM [dbo].[Reservations];

SELECT
    COUNT_BIG(*) AS [PaymentCount],
    COALESCE(SUM([Amount]), 0) AS [TotalPaymentAmount]
FROM [dbo].[Payments];

SELECT
    N'Club' AS [Entity],
    [Id],
    CONCAT(N'name=', [Name], N'; email=', COALESCE([Email], N'')) AS [FingerprintEvidence]
FROM [dbo].[Clubs]
WHERE [Id] = 1
   OR [Name] = N'Padelito'
   OR [Email] = N'admin@padelito.com'
UNION ALL
SELECT
    N'Person',
    [Id],
    CONCAT(
        N'name=', [FirstName], N' ', [LastName],
        N'; dni=', COALESCE([Dni], N''),
        N'; email=', COALESCE([Email], N''))
FROM [dbo].[People]
WHERE [Id] = 1
   OR [Id] BETWEEN 9001 AND 9004
   OR [Id] BETWEEN 9101 AND 9105
   OR [Email] IN (
        N'admin@padelito.com',
        N'maya.johnson@example.com',
        N'ethan.brooks@example.com',
        N'sofia.martinez@example.com',
        N'liam.carter@example.com',
        N'ava.thompson@example.com')
UNION ALL
SELECT
    N'User',
    [Id],
    CONCAT(N'username=', [Username], N'; employee=', [EmployeeId], N'; role=', [RoleId])
FROM [dbo].[Users]
WHERE [Id] = 1 OR [Username] = N'admin'
UNION ALL
SELECT
    N'Court',
    [Id],
    CONCAT(N'name=', [Name], N'; club=', [ClubId], N'; price=', [HourPrice])
FROM [dbo].[Courts]
WHERE [Id] BETWEEN 9001 AND 9003
   OR [Id] BETWEEN 9101 AND 9104
UNION ALL
SELECT
    N'Promotion',
    [Id],
    CONCAT(N'name=', [Name], N'; discount=', [DiscountPercentage])
FROM [dbo].[Promotions]
WHERE [Id] = 9001 OR [Id] BETWEEN 9101 AND 9102
ORDER BY [Entity], [Id];

SELECT
    r.[Id] AS [ReservationId],
    r.[ClientId],
    r.[EmployeeId],
    r.[AvailableTurnId],
    r.[ReservationDate],
    r.[FinalPrice],
    COALESCE(payment_summary.[PaymentRows], 0) AS [PaymentRows],
    COALESCE(payment_summary.[PaymentTotal], 0) AS [PaymentTotal],
    COALESCE(audit_summary.[AuditRows], 0) AS [AuditRows],
    audit_summary.[LatestAuditAt]
FROM [dbo].[Reservations] AS r
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS [PaymentRows], COALESCE(SUM(p.[Amount]), 0) AS [PaymentTotal]
    FROM [dbo].[Payments] AS p
    WHERE p.[ReservationId] = r.[Id]
) AS payment_summary
OUTER APPLY
(
    SELECT COUNT_BIG(*) AS [AuditRows], MAX(a.[CreatedAt]) AS [LatestAuditAt]
    FROM [dbo].[ReservationAudits] AS a
    WHERE a.[ReservationId] = r.[Id]
) AS audit_summary
WHERE r.[Id] BETWEEN 9001 AND 9005
   OR r.[Id] BETWEEN 9101 AND 9106
   OR r.[ClientId] BETWEEN 9001 AND 9004
   OR r.[ClientId] BETWEEN 9101 AND 9105
ORDER BY r.[Id];

SELECT
    a.[Id] AS [AuditId],
    a.[ReservationId],
    a.[Action],
    a.[Username],
    a.[CreatedAt],
    a.[Description]
FROM [dbo].[ReservationAudits] AS a
WHERE a.[Id] BETWEEN 9001 AND 9007
   OR a.[Id] BETWEEN 9101 AND 9108
   OR a.[Description] LIKE N'%demo%'
ORDER BY a.[Id];
