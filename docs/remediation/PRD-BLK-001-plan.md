# Remediation plan: PRD-BLK-001 — Club/tenant object-level authorization is incomplete

## Snapshot

- Result: `CONFIRMED`
- Finding status: `OPEN`
- Severity: `BLOCKER`
- Selected architecture: `SINGLE-TENANT — one application installation and one exclusive database per club`
- Branch: `main`
- Commit: `7986f1df158c9c252f093e7a434d3aa113df7981`
- Original plan date: `2026-07-27 11:26:16 -03:00`
- Decision update: `2026-07-27 11:49:16 -03:00` (`America/Buenos_Aires`)
- Working tree: dirty before both planning passes.
- Audit report: `docs/production-readiness.md` exists.
- Plan existed before this update: yes.
- Reproducibility warning: `POTENTIALLY NON-REPRODUCIBLE`.
- Repository modification made by this plan update: this file only.

### Approved human decisions

The user explicitly approved:

1. Padelito is permanently single-tenant: each club receives an independent application installation and exclusive database.
2. The application must fail safely if its database contains more than one club; it must never merge, delete, or automatically choose one.
3. Existing `ClubId` relationships remain as defense in depth, and queries for entities that carry/derive `ClubId` remain club-scoped.
4. `PRD-BLK-002` and `PRD-BLK-003` must be resolved before implementing this plan, so the migration/bootstrap foundation is stable.

Consequences of those decisions:

- Clients, people, promotions, court types, and other database-global records belong to the one club installation by database boundary.
- No cross-club customer sharing or membership model is required.
- DNI and username uniqueness may remain database-global because the database contains one club.
- Promotions and court types may remain installation-global and locally administered.
- No `Client.ClubId`, `Promotion.ClubId`, `ClubClient`, or tenant-data backfill migration is planned.

## Current status and confirmation

The original finding remains `CONFIRMED` because the current application does not enforce the newly approved single-tenant invariant.

Current code can store multiple `Club` rows. Startup returns immediately when bootstrap is disabled and does not validate club count. When bootstrap is enabled, it checks only whether any user exists before inserting another club/admin. If a database contains multiple clubs, the application can start and several privileged CRUD paths use global IDs.

The approved architecture changes the remediation target:

- The database boundary is the primary isolation boundary for clients, people, promotions, and reference catalogs.
- Application startup/readiness must prove that the database contains exactly one valid club.
- Existing `ClubId` relationships and scoped queries remain defense in depth for employees, users, courts, turns, reservations, payments, reports, and audits.
- Unscoped by-ID operations for entities that already have a club relationship must still be corrected.

No relevant source fingerprint from the original plan changed before this update. The branch and commit still match the audit. Because the implementation exists only in a dirty working tree, revalidation remains mandatory.

## Evidence inspected

### Audit and Git state

- `docs/production-readiness.md:35-53`: original finding, evidence, impact, correction, and verification requirement.
- `docs/production-readiness.md:1-30`: audit scope, branch, commit, topology, and safety boundary.
- Read-only commands:
  - `git branch --show-current`
  - `git rev-parse HEAD`
  - `git status --short`
  - `git status --porcelain=v1`
  - `git diff --name-status`
  - `git diff --unified=3 -- <relevant files>`
  - `git ls-files --others --exclude-standard`
  - relevant SHA-256 and last-write-time checks
- Relevant hashes rechecked in the decision update all matched the original plan.

### Authentication and server-derived club

- `backend/Padelito.Application/Services/AuthService.cs:48-74`: current user derives `ClubId` from `User.Employee.ClubId`.
- `backend/Padelito.Api/Security/JwtTokenService.cs:21-30`: signed token contains `UserId`, `EmployeeId`, role, and `ClubId`.
- `backend/Padelito.Api/Controllers/CatalogControllerBase.cs:9-21`: reads `CurrentClubId` from the authenticated principal.
- `backend/Padelito.Api/Program.cs:63-91`: validates tokens and applies role policies.

### Missing single-tenant invariant

- `backend/Padelito.Api/Program.cs:93-95`: runs `ProductionBootstrapper.InitializeAsync` before serving requests.
- `backend/Padelito.Api/Startup/ProductionBootstrapper.cs:17-29`:
  - returns without database validation when bootstrap is disabled;
  - checks for any User, not exact Club count, when bootstrap is enabled.
- `ProductionBootstrapper.cs:35-75`: inserts a new Club, Person, Employee, and User without first proving the database has zero clubs and no inconsistent application data.
- `backend/Padelito.Infrastructure/Data/PadelitoDbContext.cs:9`: exposes `DbSet<Club>` with no singleton invariant.
- `backend/Padelito.Api/Controllers/HealthController.cs:13-25`: checks only database connectivity, not single-club validity.
- There is no API endpoint that creates clubs, which reduces runtime mutation risk but does not establish the invariant for bootstrap, migration, manual data, or a misconfigured shared database.

### Confirmed defense-in-depth gaps

#### Employees and users

- Employee is club-owned by required `Employee.ClubId`.
- User is club-owned through required `User.Employee.ClubId`.
- Employee list/create are scoped, but get/update/activate/deactivate load by global ID:
  - `EmployeesController.cs:19-46`
  - `CatalogService.cs:108-145`
  - `CatalogRepository.cs:71-79`
- User list/get/create/update/password/status paths are global:
  - `UsersController.cs:13-52`
  - `CatalogService.cs:147-230`
  - `CatalogRepository.cs:81-94`
- `CreateUserAsync` can select an Employee by global ID.

#### Courts and available turns

- Court has required `Court.ClubId`.
- AvailableTurn belongs through `AvailableTurn.Court.ClubId`.
- Court get/update/status and turn create/update/status use global IDs:
  - `CourtsController.cs:19-46`
  - `AvailableTurnsController.cs:19-40`
  - `CatalogService.cs:276-367`
  - `CatalogRepository.cs:146-184`

#### Reservations

- Reservation list/get/status, payments, reports, and audits already scope through `AvailableTurn.Court.ClubId`.
- Reservation create verifies Employee and Turn/Court against the authenticated club.
- Client and Promotion are loaded globally. Under the approved single-database-per-club model this is acceptable only after the single-club database invariant is technically enforced.

### Installation-global entities

Under the approved architecture:

| Entity | Classification |
|---|---|
| Club | singleton installation root |
| Employee, User | club-owned; retain `ClubId`-scoped queries |
| Court, AvailableTurn | club-owned; retain `ClubId`-scoped queries |
| Reservation, Payment, ReservationAudit | club-owned through Court; retain scoped queries |
| Client, Person | installation-global, implicitly owned by the only club database |
| Promotion | installation-global |
| CourtType | installation-global catalog |
| Role, ReservationStatus, PaymentMethod | installation-global reference catalogs |

Frontend callers correctly send resource IDs without a client-controlled `clubId`. This must remain unchanged.

### Existing tests

- `backend/Padelito.Application.Tests/ApiIntegrationTests.cs` uses one EF InMemory database and one club.
- No test proves that startup fails with zero/multiple clubs or inconsistent Club references.
- No test runs two isolated installations to prove database-boundary isolation.
- Existing tests do not cover all foreign-club by-ID operations.

## Root cause

1. The product's deployment model was not explicitly defined.
2. The schema contains multi-club structures, while several services/repositories behave as if the database were single-club.
3. Bootstrap and startup do not enforce either architecture.
4. Role authorization is not consistently complemented by club-scoped object retrieval where `ClubId` already exists.
5. Health checks do not detect an invalid shared/multi-club database.
6. Tests do not model the approved installation boundary.

## Security and production impact

Until the invariant is enforced:

- A database can accidentally contain multiple clubs.
- Admins can read/mutate another stored club's employees, users, courts, or schedules by ID.
- Another club's user password/status can be changed.
- Global client records can be used across stored clubs.
- A misconfigured shared database silently violates the intended deployment isolation.

After remediation, the application must refuse to start/declare readiness when its exclusive database is not a valid single-club installation.

## Proposed solution

### 1. Enforce the single-club invariant before serving requests

Create a focused startup validator, either within `ProductionBootstrapper` or a dedicated `SingleTenantStartupValidator`.

After optional bootstrap and before `app.Run()`:

1. Query Club count.
2. Require exactly one Club.
3. Require that Club to be active.
4. Verify every Employee and Court references that Club.
5. Verify every User resolves through an Employee from that Club.
6. Verify every AvailableTurn resolves through a Court from that Club.
7. Fail with `InvalidOperationException` containing aggregate IDs/counts only, not personal or secret data.
8. Never select the first club, merge clubs, move records, delete records, or repair automatically.

The validation must run in every environment that uses a persistent application database, including when bootstrap is disabled. Testing may replace it only with an explicit test setup.

### 2. Make bootstrap single-tenant and fail closed

Update bootstrap rules:

- Bootstrap disabled:
  - never create data;
  - still validate that the existing database contains exactly one valid Club before serving.
- Bootstrap enabled with zero Clubs and no application-owned users/employees/courts/clients/reservations:
  - create exactly one Club and initial administrator transactionally;
  - validate the completed invariant;
  - require bootstrap to be disabled afterward.
- Bootstrap enabled with exactly one valid Club:
  - create nothing;
  - log a safe warning and validate.
- More than one Club, or zero Clubs with non-empty/ambiguous application data:
  - fail before serving;
  - require explicit operator remediation;
  - preserve all data.

Do not infer that records belong to a club merely because it is first or active.

### 3. Detect invalid state through readiness

Extend the existing `/health` behavior or introduce a focused readiness component so that it returns `503` when:

- database connectivity fails;
- Club count is not exactly one;
- the singleton Club is inactive;
- an Employee or Court references a different Club.

Do not expose Club IDs, record counts beyond a generic diagnostic category, connection information, or personal data in the public response. Detailed aggregate diagnostics belong in protected logs.

This is not a full observability redesign; broader health/liveness work remains `PRD-MAJ-004`.

### 4. Keep `ClubId` defense in depth

Add `clubId` to service and repository operations for entities that already carry or derive it:

- Employee:
  - get, update, activate, deactivate by `(id, clubId)`.
- User:
  - list and get through `Employee.ClubId`;
  - create only for Employee `(employeeId, clubId)`;
  - update/password/activate/deactivate by `(userId, Employee.ClubId)`.
- Court:
  - get, update, activate, deactivate by `(id, clubId)`.
- AvailableTurn:
  - create requires Court `(courtId, clubId)`;
  - update/status loads Turn through `Court.ClubId`;
  - update destination Court must also match.
- Reservation:
  - retain current club scoping for list/get/status/payment/report/audit;
  - retain Employee and Turn validation during create.

Prefer repository predicates containing both resource ID and club ID. Do not load globally and authorize afterward when a scoped query is possible.

### 5. Keep installation-global queries explicit

Do not add meaningless tenant columns to:

- Client or Person;
- Promotion;
- CourtType;
- Role;
- ReservationStatus;
- PaymentMethod.

Document that these entities are scoped by exclusive database installation, not shared-database tenant filters.

Client list/get/create/update/status, profile, and dashboard client count may remain database-global because startup guarantees that the database belongs to exactly one Club. Reservation Client/Promotion selection is valid only within that exclusive database.

### 6. Use non-enumerating object responses

For scoped Employee/User/Court/Turn queries:

- foreign-club and nonexistent IDs must produce the same response.
- Prefer `404` with a generic message.
- Add the minimum focused not-found mapping needed by this finding.
- Do not implement the full centralized exception policy from `PRD-MIN-002`.

### 7. Preserve deployment isolation operationally

Document the invariant in deployment/configuration guidance:

- one application instance per club;
- one exclusive database/connection string per instance;
- credentials isolated per instance;
- no shared production database;
- backups/restores performed per club;
- no central cross-club reporting or platform administration;
- migration and application version tracked per installation.

## Alternatives considered

1. **Shared-database multi-tenancy with Client/Promotion ClubId** — rejected by the approved deployment model.
2. **Shared Client membership (`ClubClient`)** — rejected; clubs do not share a database.
3. **Remove all ClubId columns** — rejected. It is a large unnecessary migration and removes useful defense in depth.
4. **Trust deployment documentation without runtime validation** — rejected. Misconfiguration must fail closed.
5. **Choose the first/active Club when multiple exist** — rejected. It can expose or corrupt data.
6. **Automatically merge/delete extra clubs** — rejected as destructive and business-unsafe.
7. **Rely only on startup count and leave existing by-ID queries global** — rejected for entities already carrying ClubId; defense-in-depth scoping is inexpensive and auditable.
8. **Add a database singleton constraint immediately** — deferred. The unsafe migration chain must first be remediated, and startup/readiness enforcement avoids inventing a data transformation in this finding.
9. **EF global query filters** — not required. Explicit scoped repository contracts are easier to audit and do not complicate background operations.

## Files expected to change

Revalidate before implementation.

### Expected

- `backend/Padelito.Api/Program.cs`
- `backend/Padelito.Api/Startup/ProductionBootstrapper.cs`
- `backend/Padelito.Api/Startup/SingleTenantStartupValidator.cs` (new, if kept separate)
- `backend/Padelito.Api/Controllers/HealthController.cs`
- `backend/Padelito.Api/Controllers/EmployeesController.cs`
- `backend/Padelito.Api/Controllers/UsersController.cs`
- `backend/Padelito.Api/Controllers/CourtsController.cs`
- `backend/Padelito.Api/Controllers/AvailableTurnsController.cs`
- `backend/Padelito.Api/Controllers/CatalogControllerBase.cs`
- `backend/Padelito.Application/Common/ResourceNotFoundException.cs` (new)
- `backend/Padelito.Application/Interfaces/Services/ICatalogService.cs`
- `backend/Padelito.Application/Interfaces/Repositories/ICatalogRepository.cs`
- `backend/Padelito.Application/Services/CatalogService.cs`
- `backend/Padelito.Infrastructure/Repositories/CatalogRepository.cs`
- affected test fakes implementing changed interfaces
- `backend/Padelito.Application.Tests/SingleTenantStartupTests.cs` (new)
- `backend/Padelito.Application.Tests/TenantIsolationApiTests.cs` (new)
- `README.md` or existing production deployment documentation

### Not expected

- `Client.cs`, `Person.cs`, `Promotion.cs`, or `CourtType.cs`
- client-facing DTOs or frontend payload types
- a `ClubClient` join entity
- a tenant ownership migration for clients/promotions
- existing migration files

If implementation discovers that a database schema change is unavoidable, stop and update this plan rather than silently adding a migration.

## Data-model implications

- Retain current Club, Employee, Court, and related FKs.
- Client/Person/Promotion/catalog ownership is the exclusive database itself.
- Retain global unique DNI and username within each installation.
- No cross-database IDs or shared customer identity is supported.
- No schema change is currently planned for `PRD-BLK-001`.

## Migration implications

- Do not edit or apply existing migrations.
- Do not generate a new migration for this finding unless revalidation proves a schema change is required.
- The current chain contains unresolved `PRD-BLK-002` and `PRD-BLK-003`; per approved sequencing, resolve those first.
- Before later deployment, generate/review the stabilized idempotent script and rehearse it under the separate remediation plans.
- If an existing database contains more than one Club, this remediation must fail closed. Data consolidation/splitting requires a separate explicit recovery plan and must never be automated here.

### Clean installation

After `PRD-BLK-002`:

- schema provisioning must produce no implicit demo club;
- explicit bootstrap creates exactly one Club/admin;
- subsequent startup validates exactly one;
- bootstrap cannot create a second Club.

### Upgrade

- Preflight must count Clubs and list aggregate inconsistency counts.
- `0` or `>1` Clubs blocks deployment.
- Existing data is preserved.
- Any database split into per-club databases is a separate operator-run migration project outside this finding.

## Compatibility implications

- Normal endpoint URLs and DTOs remain unchanged.
- Valid single-club installations continue operating.
- Invalid databases that previously started will now fail startup/readiness.
- Foreign/mismatched Employee/User/Court/Turn IDs change to generic `404`.
- Client, Promotion, CourtType, Role, Status, and PaymentMethod behavior remains installation-global.
- Deployment topology becomes an explicit supported contract: no shared database.

## Test plan

### Startup invariant

- exactly one active Club and consistent references: startup succeeds;
- zero Clubs with bootstrap disabled: startup fails;
- more than one Club: startup fails;
- one inactive Club: startup fails;
- Employee or Court referencing another Club: startup fails;
- User/Turn resolving through an inconsistent owner: startup fails;
- failures preserve every row;
- failure messages/logs contain no secrets or personal values.

### Bootstrap

- empty eligible database with bootstrap enabled creates one Club/admin transactionally;
- rerunning bootstrap creates nothing;
- bootstrap enabled against one valid existing Club creates nothing;
- bootstrap enabled against multiple Clubs fails;
- zero Clubs with pre-existing ambiguous application data fails;
- no test relies on choosing the first Club.

### Two-club isolation requirement

Model two clubs as two isolated application factories/databases:

- installation A contains Club A and records whose IDs may collide with installation B;
- installation B contains Club B and its own records;
- each instance can read/mutate only its own database;
- an ID existing only in the other database behaves as nonexistent;
- operations in A leave B unchanged and vice versa.

Also create a deliberately invalid test database containing both clubs and prove the application refuses to start/declare readiness. Do not run normal CRUD against the invalid database.

### Scoped entities

For Employee, User, Court, and AvailableTurn:

- local list/get/create/update/status/password paths succeed;
- repository/service queries include authenticated `clubId`;
- a mismatched club ID returns the same result as a missing ID;
- denied operations do not call `SaveChangesAsync`;
- turn update cannot move a Turn to a Court from a different Club.

### Installation-global entities

- Client CRUD/profile/dashboard count operate within the one installation database.
- Client and Promotion IDs from the other isolated database are nonexistent.
- Request payloads cannot select or override a Club.
- Global reference catalogs remain usable.

### Health/readiness

- valid database returns healthy;
- connectivity failure returns `503`;
- invalid Club count/inactive Club/inconsistent ownership returns `503`;
- public response is generic.

### SQL Server evidence

EF InMemory is acceptable for HTTP/startup control flow but not proof of SQL query translation or migration behavior.

Before marking `RESOLVED`, require focused SQL Server integration evidence on an isolated disposable database for:

- scoped Employee/User/Court/Turn queries;
- bootstrap transaction behavior;
- preflight Club-count queries;
- clean and invalid database startup states.

Do not apply migrations through this skill. If SQL Server evidence cannot be produced safely, keep `OPEN` or `PARTIALLY RESOLVED`.

## Verification commands

Run from repository root after implementation and record exact results.

### Drift and scope

```powershell
git branch --show-current
git rev-parse HEAD
git status --short
git diff --check
git diff --name-status
git diff -- backend/Padelito.Api backend/Padelito.Application backend/Padelito.Infrastructure backend/Padelito.Application.Tests README.md
```

### Focused tests

```powershell
dotnet build backend/PadelitoV2.slnx -c Release --no-restore
dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~SingleTenantStartup"
dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~TenantIsolation"
dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj -c Release --no-build --no-restore
```

### Static scope search

```powershell
rg -n "Get(Employee|User|Court|AvailableTurn)Async\\(int id, CancellationToken|GetUsersAsync\\(CancellationToken" backend
rg -n "FirstOrDefaultAsync\\(x => x\\.Id == id, cancellationToken\\)" backend/Padelito.Infrastructure/Repositories
rg -n "dbContext\\.Clubs|Bootstrap:Enabled|SingleTenant" backend/Padelito.Api
```

Every global query match must be classified as installation-global or corrected.

### Frontend

Frontend files are not expected to change. If they do:

```powershell
pnpm --dir frontend lint
pnpm --dir frontend build
```

### Migration review dependency

After `PRD-BLK-002` and `PRD-BLK-003` are resolved, use their approved verification commands to generate and review the latest idempotent migration script. Do not apply it through this skill.

## Required environment

- Compatible .NET 10 SDK and restored dependencies.
- Two isolated test databases/factories for the two-installation tests.
- An explicitly disposable local SQL Server for SQL-specific evidence, when safely available.
- No production/remote database credentials or real data.

## Rollback and recovery

### Application rollback

- Code rollback would remove the fail-closed invariant and is a security regression.
- If emergency rollback is unavoidable, prevent traffic and independently verify the database still contains one Club before running the older version.

### Data recovery

- This plan performs no automatic data repair.
- If startup finds multiple Clubs, preserve the database and stop.
- An operator must decide whether the database was misconfigured or must be split into separate per-club databases.
- Any split must preserve Clients, People, Reservations, Payments, and Audits and reconcile row/monetary counts under a separate approved plan.

### Backup

- Require backup/restore evidence before any real database separation or migration.
- Backups remain per club installation.

## Risks and dependencies

### Risks

- Operational teams may accidentally point two installations to the same database.
- Startup validation adds a database dependency before serving requests.
- A Club row inserted out-of-band after startup may not be detected until readiness/restart; readiness validation mitigates this.
- Existing dirty changes overlap `Program.cs`, `CatalogService`, `PadelitoDbContext`, and tests.
- Overly detailed public health errors could expose internal state.
- The application still contains multi-club-shaped schema elements; documentation and tests must keep the supported topology unambiguous.

### Dependencies

- `PRD-BLK-002` must remove unsafe/non-deterministic production provisioning and establish explicit bootstrap.
- `PRD-BLK-003` must resolve the destructive pending payment migration.
- The migration/model snapshot must be stabilized without discarding user work.
- Deployment documentation must state one connection string/database per club installation.
- SQL Server evidence must be available before final resolution.

## Assumptions

- Each production database is exclusive to exactly one club.
- There is no central platform admin or cross-club report.
- A customer appearing at two clubs is represented independently in two databases.
- DNI and username uniqueness are per database/club installation.
- Existing `ClubId` claims and relationships remain.
- No endpoint accepts a client-controlled Club owner.
- Existing user changes are intentional and must be preserved.

## Decisions requiring human input

None remain for the architecture of `PRD-BLK-001`.

Recorded decisions:

- single installation/database per club: approved;
- fail closed for more than one Club: approved;
- retain `ClubId` and scoped defense in depth: approved;
- resolve `PRD-BLK-002` and `PRD-BLK-003` first: approved.

Implementation is still dependency-blocked until those two findings and the migration base are stabilized.

## Explicit implementation boundaries

- Implement only the single-tenant invariant and confirmed defense-in-depth gaps.
- Do not add Client/Promotion tenant ownership or shared membership.
- Do not remove ClubId columns.
- Do not merge, move, duplicate, or delete tenant data.
- Do not edit/apply existing migrations.
- Do not fix `PRD-BLK-002`, `PRD-BLK-003`, centralized exceptions, observability, rate limiting, CI/CD, pagination, or unrelated frontend behavior in this implementation.
- Do not expose tenant IDs in writable DTOs.
- Do not overwrite unrelated dirty-worktree changes.

## Definition of done

`PRD-BLK-001` may be marked `RESOLVED` only when:

1. The supported topology is documented as one installation and exclusive database per club.
2. Startup validates exactly one active Club before serving requests in every persistent environment.
3. Bootstrap cannot create a second Club and fails closed on ambiguous/non-empty invalid state.
4. Readiness returns `503` for invalid single-club state without exposing internal data.
5. Employee/User/Court/Turn object queries and mutations are scoped by authenticated `ClubId`.
6. Foreign/missing scoped IDs are indistinguishable and denied mutations leave data unchanged.
7. Client/Person/Promotion/catalog behavior is explicitly documented as installation-global.
8. Two isolated club installations are tested, and a combined two-club database is rejected.
9. Focused and full backend tests/build pass.
10. SQL Server-specific evidence validates query translation and startup/bootstrap behavior.
11. `PRD-BLK-002` and `PRD-BLK-003` are resolved sufficiently to provide a reproducible clean/upgrade foundation.
12. No unrelated user changes are overwritten.
13. Verification evidence is appended here before updating the finding in `docs/production-readiness.md`.

## Implementation record

Not started.

Plan mode updated only this file. Application source, migrations, tests, and the readiness report were not modified.

## Verification evidence

Not run in plan mode.

- Static revalidation: passed; the finding remains confirmed under the newly approved architecture because the single-club invariant is not enforced.
- Relevant hash drift check: no material drift detected.
- Build/tests/lint/publish: not run.
- Database connections/migrations: not run.
