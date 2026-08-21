/* Read-only preflight for the approved public-demo privacy correction. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() IN (N'master', N'model', N'msdb', N'tempdb')
BEGIN
    THROW 51140, 'BLOCKED: select the dedicated Padelito demo database.', 1;
END;

IF (SELECT COUNT_BIG(*) FROM dbo.Clubs) <> 1
    OR (SELECT COUNT_BIG(*) FROM dbo.Clubs WHERE IsActive = 1) <> 1
BEGIN
    THROW 51141, 'BLOCKED: expected exactly one active club.', 1;
END;

IF (SELECT COUNT_BIG(*)
    FROM dbo.People AS person
    INNER JOIN dbo.Clients AS client ON client.PersonId = person.Id
    LEFT JOIN dbo.Employees AS employee ON employee.PersonId = person.Id
    WHERE person.Id = 2
      AND person.FirstName IN (N'Fernando', N'Ferdinando')
      AND person.LastName = N'Perez'
      AND person.IsActive = 1
      AND employee.Id IS NULL) <> 1
BEGIN
    THROW 51142, 'BLOCKED: Ferdinando Perez client fingerprint does not match.', 1;
END;

IF (SELECT COUNT_BIG(*)
    FROM dbo.People AS person
    INNER JOIN dbo.Employees AS employee ON employee.PersonId = person.Id
    LEFT JOIN dbo.Users AS appUser ON appUser.EmployeeId = employee.Id
    LEFT JOIN dbo.Roles AS appRole ON appRole.Id = appUser.RoleId
    INNER JOIN dbo.Clubs AS club ON club.Id = employee.ClubId
    WHERE person.Id = 3
      AND person.FirstName = N'Juan'
      AND person.LastName = N'Perez'
      AND club.IsActive = 1
      AND (appUser.Id IS NULL OR (
          appUser.Username = N'juanperez'
          AND appUser.IsActive = 1
          AND appRole.Name = N'Reception'))) <> 1
BEGIN
    THROW 51143, 'BLOCKED: juanperez Reception fingerprint does not match.', 1;
END;

IF EXISTS (
    SELECT 1
    FROM dbo.Users AS appUser
    INNER JOIN dbo.Employees AS employee ON employee.Id = appUser.EmployeeId
    WHERE appUser.Username = N'juanperez' AND employee.PersonId <> 3)
BEGIN
    THROW 51145, 'BLOCKED: username juanperez belongs to a different employee.', 1;
END;

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'jorge' AND IsActive = 1)
BEGIN
    THROW 51146, 'BLOCKED: legacy Reception user jorge must remain inactive.', 1;
END;

IF EXISTS (
    SELECT 1 FROM dbo.People
    WHERE Dni IN (N'555010198', N'555010199') AND Id NOT IN (2, 3))
BEGIN
    THROW 51144, 'BLOCKED: a fictitious Customer ID is already in use.', 1;
END;

SELECT
    N'READY' AS Result,
    DB_NAME() AS DatabaseName,
    CONVERT(bit, CASE WHEN EXISTS (
        SELECT 1 FROM dbo.People
        WHERE Id = 2 AND Dni = N'555010199'
          AND Phone = N'(917) 555-0199'
          AND Email = N'ferdinando.perez@example.com') THEN 1 ELSE 0 END) AS FerdinandoAlreadyAnonymous,
    CONVERT(bit, CASE WHEN EXISTS (
        SELECT 1 FROM dbo.People
        WHERE Id = 3 AND Dni = N'555010198'
          AND Phone = N'(917) 555-0198'
          AND Email = N'juan.perez@example.com') THEN 1 ELSE 0 END) AS JuanAlreadyAnonymous;
