# Feature Implementation Report: Demo ephemeral accounts and private administrator

## 1. Result

RELEASE CANDIDATE VALIDATED. The complete source implementation, schema
migration, tests, frontend behavior, privacy scripts, and deployment runbook are
ready. The disposable English-database rehearsal passed, including the exact
production topology discovered on 2026-08-01. Remote
MonsterASP.NET backup/restore evidence, deployment, JWT rotation, and production
smoke tests remain the final controlled operations.

## 2. Source Plan

- Source: detailed plan supplied directly in the implementation request; no
  repository plan file was used.
- Phases requested: identity/schema, provisioning, isolated session database,
  frontend experience, deployment/delivery.
- Phases completed locally: 1 through 4, plus the phase 5 runbook.
- Phase not executed: remote deployment and credential delivery.

## 3. Implemented Changes

### Backend

- Added `User.IsDemo`, exposed it through `CurrentUserDto`, and signed it into
  JWTs.
- Added a request-scoped database selector that uses SQL Server for real users
  and an isolated EF Core InMemory database for demo users.
- Added `DemoSessionMiddleware` and `DemoSessionRegistry` with mandatory UUID
  headers, per-session write serialization, 30-minute idle expiry, a 100-session
  limit, and fail-closed 400/429/503 responses.
- Added full single-club snapshot cloning while excluding non-demo `User` rows.
- Kept login, logout, production bootstrap, and lifecycle worker on SQL Server.
- Added the explicit, transactional, idempotent `DemoAccessProvisioner` for
  configured `admin`, `juanperez`, and the private `Ferdi` administrator.
- Added a fail-closed creation path for a missing `juanperez`: it requires the
  exact active Juan Perez employee, a temporary private Identity password hash,
  and no existing employee user. The legacy `jorge` account remains inactive.

### Frontend

- Added one `X-Demo-Session-Id` UUID per page load to JSON and download calls.
- Reset the UUID at logout/unauthorized-session cleanup.
- Added the demo banner, `Role · Demo` label, and `Try ...` catalog actions.
- Preserved the existing public Admin credentials on the login form.

### Database

- Added migration `20260801181253_AddDemoUserFlag`, containing only a new
  non-null `Users.IsDemo` bit column with default `false`.

### Tests

- Added provisioner creation, idempotency, disabled-mode, and no-partial-state
  tests.
- Added integration coverage for Admin and Reception isolation, reload/browser
  separation, missing-header failure, the complete client/turn/reservation/
  payment/report/operations/dashboard/audit flow, non-demo user exclusion, and
  real private-admin persistence.

### Configuration and documentation

- Added non-secret `DemoMode` limits and a disabled-by-default provisioning
  switch.
- Added `docs/operations/demo-access-rollout.md` and linked it from the README.

## 4. Plan Deviations

- Original: add the InMemory provider in infrastructure. Actual: the package is
  referenced by the API host where the session registry lives; infrastructure
  exposes only the context-selection abstraction. Classification: minor. Impact:
  same runtime behavior with a narrower dependency boundary.
- Original: show a specific notice when an idle session expires. Actual: the
  next request safely creates a clean snapshot and the persistent demo banner
  explains reset behavior, but there is no distinct expiry toast.
  Classification: minor. Impact: no security or persistence change.
- Original: create a plaintext credential file. Actual: created outside Git at
  `C:\Users\Ferdinando\Desktop\Padelito-admin-privado.txt` with inheritance
  disabled and an ACL restricted to the current Windows user. The password is
  not included in source, logs, or this report.
- Original: deploy and provision MonsterASP.NET. Actual: not attempted because
  no remote migration, infrastructure mutation, or secret modification was
  authorized by the implementation environment. Classification: material
  operational omission. Impact: source is ready, but deployed accounts are not
  changed yet.

## 5. Validation Performed

| Command or check | Result | Notes |
| ---------------- | ------ | ----- |
| `dotnet tool restore` | PASS | Restored repository-local EF tool with approval. |
| `dotnet restore backend\PadelitoV2.slnx` | PASS | Restored after adding EF InMemory. |
| `dotnet build backend\PadelitoV2.slnx --no-restore` | PASS | Backend compiled without warnings/errors beyond the SDK preview notice. |
| Focused `DemoAccessProvisionerTests` | PASS | 3 tests. |
| Focused `DemoSessionIntegrationTests` | PASS | 4 tests after final expansion. |
| `dotnet test backend\PadelitoV2.slnx --no-restore` | PASS | 74 passed, 0 failed. |
| `npm.cmd run lint` | PASS | oxlint. |
| `npm.cmd run build` | PASS | TypeScript and Vite production build. |
| `git diff --check` | PASS | No whitespace errors. |
| Migration source inspection | PASS | Idempotent script contains the expected `AddColumn`; no drop or delete statements. |
| Disposable English database rehearsal | PASS | Privacy preflight/update/postflight, migrations, provisioning, demo reset, and private persistence verified. |

The final suite after adapting the observed production topology passed 74 tests.
The second rehearsal started from `Fernando Perez`, inactive Juan Perez without
a user, and inactive `jorge`; it completed with the approved anonymous profiles,
preserved Reception hash, `admin`/`juanperez` demo flags, and real `Ferdi`.

## 6. Test Coverage Added

Coverage protects the server-side persistence boundary rather than only UI
visibility. It proves demo writes remain queryable within one session, affect
derived modules, disappear with a new UUID, stay isolated across browsers, and
never reach the production context. It also proves the real administrator takes
the SQL path. Session-capacity/TTL timing is covered by bounded implementation
and configuration validation but does not have a clock-driven integration test.

## 7. Database Changes

- Entity and snapshot add `Users.IsDemo`.
- Migration adds one non-null bit column with default `false` and drops only that
  column in `Down`.
- All migrations were applied only to the disposable `PADELITO_V2_PREVIEW`
  rehearsal database; production remains untouched at this checkpoint.
- No existing rows are updated by the migration; account classification occurs
  only through the explicit startup provisioner.
- Rollback must not expose published demo credentials to an older application.

## 8. Security Review

- Persistence routing is based on a signed server claim, not frontend state.
- Demo sessions fail closed without a valid UUID and cannot use SQL Server.
- Writes are serialized per session to protect concurrency-sensitive checks.
- Non-demo user rows are excluded from public session copies.
- Provisioning validates topology and identities before committing atomically.
- Private passwords are accepted only from configuration, never logged or
  included in source, migration, API response, frontend bundle, or this report.
- The Reception password hash is accepted only when the approved account is
  missing, validated as an Identity hash, copied from the local English base,
  and removed from hosting configuration after provisioning.
- Production rollout requires JWT-key rotation to invalidate legacy cookies.

## 9. Remaining Work

### Required

- Execute `docs/operations/demo-access-rollout.md` in a maintenance window only
  after proving a backup can be restored into an isolated database.
- Apply the migration, set the private provisioning secret, rotate `Jwt__Key`,
  deploy, and run the documented smoke tests.
- Disable provisioning and remove its password secret after verification.
- Keep the delivered credential file private and remove the temporary hosting
  password secret after provisioning.

### Optional

- Add a dedicated toast when an idle demo session is recreated.
- Add a clock-driven cleanup/capacity test.

## 10. Deferred Findings

- The repository already contained unrelated changes to the English demo SQL
  seed and untracked remediation/operations artifacts. They were preserved and
  not modified by this implementation except for the new, separate rollout
  document.
- The project uses a preview .NET SDK, which emits `NETSDK1057` informational
  messages.

## 11. Manual Verification

1. Apply the migration to a disposable production-like database.
2. Seed existing `admin` and `juanperez` accounts, enable provisioning with a
   test secret, and start once.
3. Log in as Admin demo, create a client, time slot, reservation, and payment;
   verify dashboard, operations, report, CSV, and audit.
4. Reload and confirm all changes disappear.
5. Repeat a write with Reception and reload.
6. Log in as `Ferdi`, make a reversible write, reload, and confirm persistence.
7. Verify only the two public users have `IsDemo=true` in SQL Server.
8. Disable provisioning, remove the secret, restart, and repeat login smoke
   tests with a freshly rotated JWT key.

## 12. Final Status

The requested source scope is complete, builds and tests pass, and the migration
is ready for review but remains unapplied. The feature is ready for code review
and controlled deployment; it is not declared production-ready until the remote
backup, rollout, JWT rotation, smoke tests, and rollback checks are completed.
