/* Read-only postflight for the public-demo privacy correction. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF NOT EXISTS (
    SELECT 1 FROM dbo.People
    WHERE Id = 2
      AND FirstName = N'Ferdinando'
      AND LastName = N'Perez'
      AND Dni = N'555010199'
      AND Phone = N'(917) 555-0199'
      AND Email = N'ferdinando.perez@example.com')
BEGIN
    THROW 51148, 'FAILED: Ferdinando privacy postcondition does not match.', 1;
END;

IF COL_LENGTH(N'dbo.Users', N'IsDemo') IS NULL
BEGIN
    THROW 51149, 'FAILED: Users.IsDemo is missing.', 1;
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.People AS person
    INNER JOIN dbo.Employees AS employee ON employee.PersonId = person.Id
    INNER JOIN dbo.Users AS appUser ON appUser.EmployeeId = employee.Id
    INNER JOIN dbo.Roles AS appRole ON appRole.Id = appUser.RoleId
    WHERE person.Id = 3
      AND person.FirstName = N'Juan'
      AND person.LastName = N'Perez'
      AND person.Dni = N'555010198'
      AND person.Phone = N'(917) 555-0198'
      AND person.Email = N'juan.perez@example.com'
      AND person.IsActive = 1
      AND appUser.Username = N'juanperez'
      AND appUser.IsActive = 1
      AND appUser.IsDemo = 1
      AND appRole.Name = N'Reception')
BEGIN
    THROW 51149, 'FAILED: Juan privacy and Reception postcondition does not match.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'admin' AND IsActive = 1 AND IsDemo = 1)
BEGIN
    THROW 51150, 'FAILED: admin is not an active demo user.', 1;
END;

IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'Ferdi' AND IsActive = 1 AND IsDemo = 0)
BEGIN
    THROW 51151, 'FAILED: Ferdi is not an active persistent administrator.', 1;
END;

IF EXISTS (SELECT 1 FROM dbo.Users WHERE Username = N'jorge' AND IsActive = 1)
BEGIN
    THROW 51152, 'FAILED: legacy Reception user jorge became active.', 1;
END;

SELECT N'PASS' AS Result, DB_NAME() AS DatabaseName;
