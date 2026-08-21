# Production Readiness Audit

- Audit date: 2026-07-27 10:25-10:35 (America/Buenos_Aires)
- Repository commit: `7986f1df158c9c252f093e7a434d3aa113df7981`
- Branch: `main`
- Audited scope: the current local working tree, including 38 pre-existing modified tracked files and 10 pre-existing untracked source/migration files
- Previous report: none
- Safety boundary: no deployment, server startup, database connection, migration application, remote service access, dependency restore, or secret-value disclosure

## Production readiness verdict: NOT READY

The application compiles, all 58 backend tests pass, the frontend passes lint and its production build, and the integrated publish command succeeds. Those are useful but insufficient signals.

Deployment is blocked by three directly evidenced risks:

1. Object-level authorization does not consistently enforce the authenticated club boundary.
2. The migration chain is incompatible with clean production provisioning and includes unconditional portfolio demo data.
3. A pending migration deletes payment records that do not match a new invariant, without a preservation, reconciliation, backup, or approval strategy.

Production-critical operational evidence is also missing: there is no CI/CD gate, release/rollback runbook, verified backup/restore procedure, production-environment smoke test, SQL Server migration rehearsal, load test, or remotely sourced dependency-vulnerability check.

## Architecture and runtime topology

- Single deployable ASP.NET Core application targeting `net10.0`; it serves controllers and the compiled SPA from the same origin.
- React 19, TypeScript 6, Vite 8 frontend; `Padelito.Api.csproj` runs the frontend build during publish and copies `frontend/dist` into `wwwroot`.
- Application, Domain, Infrastructure, API, and xUnit test projects.
- EF Core 10 with SQL Server and code-first migrations.
- JWT authentication read from an `HttpOnly`, `SameSite=Strict`, production-`Secure` cookie. Policies: `AdminOnly`, `AdminOrReception`, and `AuthenticatedStaff`.
- A hosted worker reconciles reservation state every minute. Reservation and payment writes use SQL transactions and database uniqueness constraints.
- Required production configuration names: `ConnectionStrings__PadelitoDb`, `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `AllowedHosts`; operational settings include `Jwt__ExpirationMinutes`, `Club__TimeZone`, and `Bootstrap__Enabled`.
- If bootstrap is enabled, it additionally requires `Bootstrap__ClubName`, `Bootstrap__AdminUsername`, `Bootstrap__AdminPassword`, `Bootstrap__AdminFirstName`, `Bootstrap__AdminLastName`, `Bootstrap__AdminDni`, `Bootstrap__AdminPhone`, and `Bootstrap__AdminEmail`.

## Blockers

### PRD-BLK-001 — Club/tenant object-level authorization is incomplete

- Severity: `BLOCKER`
- Area: authentication, authorization, tenant isolation, privileged CRUD
- Status: `OPEN`
- Problem: many list and object-by-ID operations do not receive or filter by the authenticated `ClubId`. The client model has no club ownership field. Admin authorization therefore proves role, but not ownership of the requested object.
- Evidence:
  - `backend/Padelito.Domain/Entities/Client.cs:3-9` has no `ClubId`.
  - `backend/Padelito.Api/Controllers/ClientsController.cs:13-52` passes `CurrentClubId` only to the profile endpoint, not list/get/create/update/activation operations.
  - `backend/Padelito.Infrastructure/Repositories/CatalogRepository.cs:15-23` lists and loads clients globally.
  - `backend/Padelito.Api/Controllers/EmployeesController.cs:19-46` and `backend/Padelito.Api/Controllers/UsersController.cs:13-52` omit `CurrentClubId` on object access and mutations.
  - `backend/Padelito.Infrastructure/Repositories/CatalogRepository.cs:71-88` loads employees and users by global ID and lists all users.
  - `backend/Padelito.Application/Services/CatalogService.cs:126-230` updates employees/users and changes passwords/status without a club ownership check.
  - `backend/Padelito.Application/Services/CatalogService.cs:276-320` gets and mutates courts by ID without validating the authenticated club.
  - `backend/Padelito.Application/Services/CatalogService.cs:328-367` creates/moves/mutates turns using global court/turn IDs.
  - `backend/Padelito.Application/Services/ReservationService.cs:147-170` validates the court's club but accepts a globally loaded client; `backend/Padelito.Infrastructure/Repositories/ReservationRepository.cs:46-53` loads client/employee/turn by global ID.
- Production impact: an authenticated user can read or mutate another club's personal data and operational configuration, change another club user's password/status, or associate another club's customer with a reservation. This is a confidentiality and integrity breach.
- Recommended correction: establish explicit ownership for every tenant-owned entity, include `clubId` in every tenant-scoped repository query and service contract, reject cross-club IDs, and apply database constraints where practical. Treat global reference catalogs separately and document them.
- Verification: add API integration tests with two clubs covering every list/get/create/update/activate/deactivate/change-password route and reservation creation with a foreign client; all cross-club attempts must return 404 or 403 and leave data unchanged.

### PRD-BLK-002 — The migration chain cannot safely provision a clean production database

- Severity: `BLOCKER`
- Area: database migrations, deployment reproducibility, environment parity
- Status: `PARTIALLY RESOLVED`
- Problem: `PrepareProductionData` deletes the seeded club, administrator, employee, and demo records. A later migration, `AddUsPortfolioDemoSeed`, requires an active club and administrator and throws when they are absent. Application bootstrap cannot fix this because bootstrap runs after migrations/application startup. The later migration also unconditionally inserts portfolio/demo business data into whichever active club is selected.
- Evidence:
  - `backend/Padelito.Infrastructure/Data/Migrations/20260721232908_PrepareProductionData.cs:16-194` deletes payments, audits, user 1, reservations, turns, clients, employee 1, courts, people, and club 1.
  - `backend/Padelito.Infrastructure/Data/Migrations/20260724190000_AddUsPortfolioDemoSeed.cs:17-27` selects an existing club/user and throws if either is missing.
  - `backend/Padelito.Infrastructure/Data/Migrations/20260724190000_AddUsPortfolioDemoSeed.cs:29-105` inserts demo courts, people, clients, promotions, turns, reservations, payments, and audit records.
  - `backend/Padelito.Api/Program.cs:93-95` runs bootstrap only after the application is built, so it cannot satisfy a prerequisite inside the migration chain.
- Production impact: clean provisioning can fail partway through; an existing database may receive fabricated customer and financial data. Deployment cannot be considered repeatable or safe.
- Recommended correction: remove environment-specific demo data from the production migration chain. Make schema migrations self-contained and deterministic. Provision the first club/admin through a separate, explicit, audited bootstrap step after schema migration.
- Verification: generate and review an idempotent migration script, then rehearse the complete migration chain against both an empty disposable SQL Server database and an anonymized production-like snapshot. Confirm zero demo rows and successful bootstrap.
- Remediation verification (2026-07-27):
  - `PASS`: a clean disposable SQL Server LocalDB database applied all 10 migrations, retained only the four global reference catalogs, and left every application-owned table empty.
  - `PASS`: explicit bootstrap created exactly one Club/Person/Employee/User graph; a second invocation was idempotent; upgrading an existing legitimate graph added no clients, courts, turns, promotions, reservations, payments, or audits.
  - `PASS`: build completed with 0 warnings/errors; focused tests passed 7/7; full backend tests passed 63/63; generated SQL contains no PRD-BLK-002 demo inserts, known demo credentials, or `THROW 51010`.
  - `FAIL`: the required idempotent SQL rehearsal cannot finish its first run. SQL Server 2019 reports error `156` near `OR` because `CREATE OR ALTER TRIGGER` is emitted inside EF's idempotent `IF` wrapper. Until that DDL is made idempotent and the script passes twice, the finding cannot be marked `RESOLVED`.
  - No remote database was accessed or changed. All local verification databases and temporary SQL files were deleted.

### PRD-BLK-003 — A migration deletes financial records to enforce the new payment model

- Severity: `BLOCKER`
- Area: data integrity, payments, migration safety, auditability
- Status: `RESOLVED`
- Problem: the pending `EnforceFullReservationPayment` migration deletes every payment for reservations whose payment count or sum does not exactly match `FinalPrice`, then creates a unique index. There is no archival table, reconciliation output, operator approval gate, or documented backup/restore plan.
- Evidence: `backend/Padelito.Infrastructure/Data/Migrations/20260724201719_EnforceFullReservationPayment.cs:13-35`.
- Production impact: legitimate partial, duplicate, adjusted, or historically inconsistent payment records can be irreversibly lost, corrupting financial history and audit trails.
- Recommended correction: replace deletion with a preflight report and a reviewed reconciliation/mapping strategy. Preserve original records in an immutable archive or transform them transactionally according to approved business rules. Require a verified backup and restore checkpoint.
- Verification: run the migration script against a production-like snapshot; reconcile row counts and monetary totals before/after, review every exception, verify rollback/restore, and obtain business-owner approval.
- Remediation verification (2026-07-27):
  - Status: `RESOLVED` against branch `main`, commit `7986f1df158c9c252f093e7a434d3aa113df7981`, with evidence tied to the current dirty working tree.
  - Replaced the destructive payment deletion with a transactional compatibility guard; added a read-only exception preflight, migration-operation regression tests, backup/cutover documentation, and approved fail-closed operating rules.
  - SQL Server 2019 LocalDB clean install passed with zero payments, a unique reservation index, and the migration recorded.
  - Compatible upgrade preserved exact reservation/payment/audit counts, monetary totals, payment fields/checksum, and accepted zero-payment plus one-exact-payment reservations. A second direct-SQL payment was rejected without changing state; rollback restored the non-unique index without changing data.
  - Five incompatible cases (single payment `0.01` below/above; multiple payments totaling exactly, below, and above `FinalPrice`) all produced preflight evidence and caused guard `51003`. Payment rows, totals, checksum, audits, index, and migration history remained unchanged.
  - `COPY_ONLY` backup with checksum, `RESTORE VERIFYONLY`, isolated restore, restored preflight, and restored migration passed with identical financial evidence. The isolated idempotent PRD-BLK-003 segment also passed twice without changing data.
  - Build passed with 0 warnings/errors; focused tests passed 2/2; full backend tests passed 65/65; the full generated idempotent SQL contained one guard and zero destructive DML operations against `Payments`.
  - The user confirmed this is a demo MVP and the migration ID was absent from every persistent/shared database. No production/remote database was accessed. Every disposable database and temporary SQL/backup/data/log file was removed.
  - Remaining limitation outside this finding: the complete idempotent chain still cannot execute because of the trigger-wrapper defect tracked by `PRD-BLK-002`; the PRD-BLK-003 segment itself is verified.

## Major findings

### PRD-MAJ-001 — No automated release gate or reproducible toolchain

- Severity: `MAJOR`
- Area: CI/CD, dependency pinning, release reproducibility
- Status: `OPEN`
- Problem: `.github/workflows` is empty; there is no repository CI pipeline. The .NET SDK is not pinned (`global.json` absent), NuGet lockfiles are absent, and both npm and pnpm lockfiles are committed while publish explicitly uses npm. This audit selected `10.0.400-preview...` and emitted `NETSDK1057`.
- Evidence: `backend/Padelito.Api/Padelito.Api.csproj:31-39`; `frontend/package-lock.json`; `frontend/pnpm-lock.yaml`; no tracked file under `.github/workflows`; baseline `dotnet --info`.
- Production impact: results can vary by machine and unreviewed code can be published without enforced build/test/lint/security gates.
- Recommended correction: pin a supported stable .NET SDK, select one frontend package manager, use frozen/locked restoration, add NuGet lockfiles or central package management, and add CI gates for restore, Release build, tests, lint, frontend build, migration checks, secret scanning, and dependency review.
- Verification: a clean ephemeral CI worker must reproduce the artifact from the commit and block a deliberately failing test/lint/migration check.

### PRD-MAJ-002 — Public login has no abuse controls

- Severity: `MAJOR`
- Area: authentication
- Status: `OPEN`
- Problem: login validates credentials but there is no rate limiter, progressive delay, lockout, IP/account throttling, or security-event alerting. Tokens default to 480 minutes.
- Evidence: `backend/Padelito.Api/Controllers/AuthController.cs:13-31`, `backend/Padelito.Application/Services/AuthService.cs:16-45`, `backend/Padelito.Api/Security/JwtTokenService.cs:18-19`; repository-wide search found no rate-limiting/lockout registration.
- Production impact: credential stuffing and brute-force attempts can run without an application-level bound; stolen tokens remain valid for a long window and have no revocation mechanism.
- Recommended correction: add ASP.NET Core rate limiting for login, account/IP backoff or lockout with safe anti-enumeration behavior, alertable failed-login events, and a reviewed session lifetime/revocation policy.
- Verification: automated tests must demonstrate 429/backoff behavior, non-enumerating responses, recovery, and bounded session lifetime.

### PRD-MAJ-003 — Release, migration, rollback, backup, and disaster-recovery operations are undocumented and unverified

- Severity: `MAJOR`
- Area: deployment operations
- Status: `OPEN`
- Problem: README contains a local migration command but no production runbook, preflight checks, backup/restore instructions, migration ordering, rollback decision tree, immutable artifact promotion, or disaster-recovery objectives.
- Evidence: `README.md:208-218`; no deployment workflow, Docker/hosting manifest, or rollback/backup documentation was found.
- Production impact: operators cannot safely recover from a failed migration or release, and successful source compilation does not establish recoverability.
- Recommended correction: document and automate a reviewed release process with database backup/restore proof, migration preflight, application/database compatibility window, smoke checks, rollback/roll-forward criteria, artifact identity, and RPO/RTO.
- Verification: rehearse the runbook in an isolated production-like environment and record recovery timings and evidence.

### PRD-MAJ-004 — Production observability is below an operable baseline

- Severity: `MAJOR`
- Area: logging, monitoring, health, tracing, alerting
- Status: `OPEN`
- Problem: only simple console logging and a database-dependent `/health` endpoint are configured. There is no separate liveness/readiness model, correlation ID, metrics, tracing, sensitive-data redaction policy, alert definitions, or external log/telemetry sink.
- Evidence: `backend/Padelito.Api/Program.cs:18-23`; `backend/Padelito.Api/Controllers/HealthController.cs:8-27`; repository-wide search found no OpenTelemetry, metrics, correlation, or alert configuration.
- Production impact: failures, latency regressions, authentication attacks, worker errors, and data-integrity incidents may not be detected or diagnosable quickly.
- Recommended correction: define structured logs and redaction, request correlation, traces, latency/error/business metrics, separate liveness/readiness checks, dashboards, alerts, and retention/access policies.
- Verification: inject controlled failures in a non-production environment and prove that signals correlate across API/database/worker paths and trigger actionable alerts.

### PRD-MAJ-005 — Several high-growth reads and background reconciliations are unbounded

- Severity: `MAJOR`
- Area: performance, database load, concurrency
- Status: `OPEN`
- Problem: clients, users, payments, reservations, reports, and audit queries return lists without pagination. Reservation reconciliation runs every minute and is also invoked by reservation reads; it takes serializable transactions and loads matching reservations with related data.
- Evidence: `backend/Padelito.Infrastructure/Repositories/CatalogRepository.cs:15-23,81-88`; `backend/Padelito.Infrastructure/Repositories/ReservationRepository.cs:13-20`; `backend/Padelito.Infrastructure/Repositories/PaymentRepository.cs:12-36`; `backend/Padelito.Api/Startup/ReservationLifecycleWorker.cs:15-35`; `backend/Padelito.Infrastructure/Repositories/ReservationLifecycleRepository.cs:17-65`. Repository-wide pagination search found only bounded dashboard activity.
- Production impact: response size, memory, query duration, locking, and multi-instance contention grow with data volume, potentially causing timeouts or availability loss.
- Recommended correction: add enforced pagination and maximum date ranges; index verified query paths; process lifecycle transitions in bounded batches with an ownership/lease strategy; avoid serializable work on read paths where possible.
- Verification: profile SQL and run concurrent load tests against a production-sized disposable database, including multiple application instances and lifecycle-worker overlap.

## Minor findings

### PRD-MIN-001 — JWT is returned in the login response body

- Severity: `MINOR`
- Area: session security
- Status: `OPEN`
- Problem: the server both writes the JWT to an `HttpOnly` cookie and returns it in `AuthResponseDto`, unnecessarily exposing it to JavaScript during login.
- Evidence: `backend/Padelito.Api/Controllers/AuthController.cs:25-31`, `backend/Padelito.Application/Services/AuthService.cs:40-45`.
- Production impact: an XSS or compromised frontend dependency active during login has an easier token-exfiltration path.
- Recommended correction: return only expiry/user metadata and keep the token solely in the cookie.
- Verification: API tests assert no token field/body secret while authenticated requests still work through the cookie.

### PRD-MIN-002 — No centralized exception/problem-details policy

- Severity: `MINOR`
- Area: safe failure behavior
- Status: `OPEN`
- Problem: expected business exceptions are translated in a controller base class, but no central exception handler or consistent problem-details contract exists.
- Evidence: `backend/Padelito.Api/Controllers/CatalogControllerBase.cs:23-69`; no `UseExceptionHandler`/`IExceptionHandler` registration found.
- Production impact: error shape and logging can vary; unexpected failures are harder to correlate and clients must handle inconsistent responses.
- Recommended correction: add a centralized production-safe exception handler with stable problem codes and correlation identifiers.
- Verification: integration tests cover validation, conflict, authorization, not-found, cancellation, and unexpected exceptions without stack/data disclosure.

### PRD-MIN-003 — Frontend asset size has no enforced budget

- Severity: `MINOR`
- Area: frontend performance
- Status: `OPEN`
- Problem: production build emitted a 481.58 kB JavaScript bundle (138.60 kB gzip) and a 953.24 kB hero image; no bundle budget or performance gate exists.
- Evidence: `npm.cmd run build` output.
- Production impact: slower first load on mobile/limited networks.
- Recommended correction: optimize the hero image, lazy-load routes/features where useful, and enforce a bundle/performance budget in CI.
- Verification: compare compressed artifact sizes and run repeatable Lighthouse/Web Vitals checks.

## Informational findings

### PRD-INF-001 — Production configuration has useful fail-fast checks

- Severity: `INFORMATIONAL`
- Area: configuration
- Status: `OPEN`
- Evidence: `backend/Padelito.Api/Program.cs:152-195` rejects missing/local/integrated SQL configuration, short/development JWT keys, absent issuer/audience, and wildcard hosts in Production.
- Note: committed defaults were inspected only by key/shape; suspected values were not printed. They include a development-key marker and wildcard host, so correct environment overrides are mandatory.

### PRD-INF-002 — Core financial/reservation writes have meaningful integrity controls

- Severity: `INFORMATIONAL`
- Area: transactions and concurrency
- Status: `OPEN`
- Evidence: unique active-slot constraint in `backend/Padelito.Infrastructure/Data/PadelitoDbContext.cs:223-260`; unique payment-per-reservation constraint at `:273-292`; serializable payment write and duplicate handling in `backend/Padelito.Infrastructure/Repositories/PaymentRepository.cs:39-99`; reservation conflict mapping in `backend/Padelito.Infrastructure/Repositories/ReservationRepository.cs:119-131`.

### PRD-INF-003 — Browser-facing baseline controls are present

- Severity: `INFORMATIONAL`
- Area: HTTP security
- Status: `OPEN`
- Evidence: secure/HttpOnly/Strict cookie configuration at `backend/Padelito.Api/Security/AuthCookie.cs:7-34`; HSTS and security headers at `backend/Padelito.Api/Program.cs:103-119`; development-only CORS at `:47-55,120-123`.

## Resolved findings retained from previous reports

None; no previous `docs/production-readiness.md` existed.

## Checks that passed

- `PASS` Backend Release build: all projects compiled, zero warnings and zero errors (the SDK separately emitted preview informational message `NETSDK1057`).
- `PASS` Backend automated tests: 58/58 passed in 11.19 seconds.
- `PASS` Frontend lint: oxlint exited 0.
- `PASS` Frontend TypeScript/Vite production build: exited 0; 1,936 modules transformed.
- `PASS` Integrated publish: API and frontend published successfully to the ignored local `bin/Release/net10.0/publish` directory.
- `PASS` Frontend dependency tree: `npm ls --depth=0` exited 0.
- `PASS` npm offline audit: reported 0 vulnerabilities from locally available advisory data.
- `PASS` Authentication required on non-auth/non-health controllers by static route inspection.
- `PASS` Server-side policy split exists for Admin, Reception, and authenticated staff.
- `PASS` Production cookie flags, HSTS, same-origin production posture, CSP, referrer policy, content-type protection, and permissions policy are configured.
- `PASS` Database health endpoint catches failures and returns 503 without returning exception details.
- `PASS` Build/test/frontend/publish commands introduced no new tracked-file mutation; only ignored intermediates changed.

## Checks not performed or not verified

- `NOT VERIFIED` Actual production configuration, secret injection, DNS, TLS termination, proxy forwarding, host filtering, filesystem permissions, and startup: no production environment was accessed. Consequence: successful startup and secure network behavior are unknown.
- `NOT VERIFIED` Migration execution or generated SQL against SQL Server: applying migrations was prohibited. Static inspection found blockers. `dotnet-ef migrations has-pending-model-changes` could not run because the local manifest tool was not restored.
- `NOT VERIFIED` Backup/restore and rollback: no procedure or isolated production-like database was available.
- `NOT VERIFIED` SQL Server-specific integration, locking, deadlock recovery, query plans, indexes under realistic cardinality, and multiple worker instances: tests use EF InMemory.
- `NOT VERIFIED` NuGet vulnerability status and package provenance: package inventory attempted to read a sandbox-inaccessible user `NuGet.Config`; no remote advisory lookup was allowed.
- `NOT VERIFIED` Current remote npm advisories: only `npm audit --offline` was permitted.
- `NOT VERIFIED` Code coverage: no coverage configuration or threshold was found.
- `NOT VERIFIED` Browser end-to-end behavior, accessibility, and cross-browser support: no E2E suite was found and no server/browser was started.
- `NOT VERIFIED` External logs, metrics, traces, alert delivery, on-call response, retention, and redaction: no configuration/evidence exists.
- `NOT VERIFIED` Email, payments-provider, queue, cache, or other integrations: none are evidenced as implemented.
- `NOT VERIFIED` Production data retention, privacy, legal/compliance requirements, and audit-record immutability.

## Recommended remediation order

1. Fix club ownership and object-level authorization across every entity/route; add two-club negative integration tests.
2. Redesign the migration chain: remove demo data from schema migrations and replace payment deletion with an approved reconciliation/preservation migration.
3. Rehearse clean and upgrade migrations against disposable SQL Server databases with verified backup/restore.
4. Establish a pinned, frozen, CI-enforced toolchain and immutable artifact pipeline.
5. Add login abuse controls and review session lifetime/revocation.
6. Add production observability, release/rollback/DR runbooks, and operational smoke checks.
7. Add pagination/batching and validate concurrency/performance at production-like scale.
8. Re-run this audit from a clean candidate commit and production-like environment evidence.

## Commands executed and concise results

All commands ran from repository root unless another working directory is shown. Discovery commands were read-only and values suspected to be secrets were redacted.

| Command | Working directory | Exit | Result |
| --- | --- | ---: | --- |
| Repository audit guidance loaded from the local skill registry | repository root | 0 | Audit rules loaded. |
| `Get-ChildItem -Force ...; rg --files ...; git status --short --branch; git log -1 ...` | repository root | 0 | Stack/files discovered; dirty baseline and commit captured. |
| `Get-Content`/`rg -n` targeted inspections of project files, configuration key shapes, controllers, services, repositories, migrations, tests, README, and `.gitignore` | repository root | 0 except one malformed final `rg` expression | Architecture, auth, tenant, migration, performance, deployment, and secrets metadata inspected. No secret values retained in evidence. |
| `git branch --show-current; git rev-parse HEAD; git status --short; git diff --stat; dotnet --version; dotnet --info; node --version; npm --version; ...` | repository root | 0 overall; `npm.ps1` subcommand blocked by local execution policy | `main`, commit captured; .NET preview SDK and Node `v24.18.0`; dependencies/assets present. Later npm commands used `npm.cmd`. |
| `dotnet build PadelitoV2.slnx -c Release --no-restore` | `backend` | 0 | Build succeeded; 0 warnings, 0 errors; preview SDK informational message. |
| `git status --short` | repository root | 0 | No new tracked mutations after backend build. |
| `dotnet test PadelitoV2.slnx -c Release --no-restore --no-build --logger "console;verbosity=normal"` | `backend` | 0 | 58 passed, 0 failed. |
| `git status --short; npm.cmd --version; npm.cmd run lint; npm.cmd run build` | `frontend` | 0 | npm 11.16.0; lint and production build passed. |
| `git status --short; dotnet publish Padelito.Api/Padelito.Api.csproj -c Release --no-restore` | `backend` | 0 | Integrated API+SPA publish succeeded; no new tracked mutations. |
| `dotnet tool run dotnet-ef migrations has-pending-model-changes --project Padelito.Infrastructure --startup-project Padelito.Api --no-build` | `backend` | 1 | `NOT VERIFIED`: local `dotnet-ef` tool was not restored. |
| `dotnet list PadelitoV2.slnx package --include-transitive --no-restore` | `backend` | 1 | `NOT VERIFIED`: sandbox denied reading user `NuGet.Config`; no escalation used to avoid secret/config access. |
| `npm.cmd ls --depth=0; npm.cmd audit --offline --omit=dev` | `frontend` | 0 | Dependency tree valid; offline advisory cache reported 0 vulnerabilities. |
| `git status --short` | repository root | 0 | Baseline changes remained; report was the only intentional new repository file. |

## Final deployment checklist

- [x] Backend Release build passes.
- [x] Backend automated tests pass (58/58).
- [x] Frontend lint and production build pass.
- [x] Integrated local publish artifact builds.
- [x] Production fails fast on obvious default/weak connection, JWT, and host configuration.
- [ ] Club/tenant ownership is enforced on every object and mutation.
- [ ] Clean-database migration chain succeeds without demo data.
- [ ] Payment migration preserves and reconciles all financial history.
- [ ] Production-like migration upgrade is rehearsed and approved.
- [ ] Backup and restore are tested with recorded RPO/RTO.
- [ ] Stable .NET SDK and a single frontend package manager are pinned.
- [ ] CI/CD gates build, tests, lint, migration safety, secrets, and dependencies.
- [ ] Login abuse controls and session revocation/lifetime policy are implemented.
- [ ] Pagination, batching, and production-scale load/concurrency tests pass.
- [ ] Structured logs, redaction, correlation, metrics, traces, readiness/liveness, and alerts are operational.
- [ ] Release, rollback/roll-forward, disaster recovery, and smoke-test runbooks are rehearsed.
- [ ] Production configuration, proxy/TLS/host behavior, and secret injection are verified.
- [ ] Current NuGet and npm advisory checks pass using authoritative online sources.

Deployment decision: **do not deploy this revision to production**.
