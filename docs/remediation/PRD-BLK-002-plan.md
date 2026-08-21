# Remediation plan: PRD-BLK-002 — The migration chain cannot safely provision a clean production database

## Snapshot

- Result: `CONFIRMED`
- Finding status: `OPEN`
- Branch: `main`
- Commit: `7986f1df158c9c252f093e7a434d3aa113df7981`
- Working tree: dirty, with substantial tracked and untracked work predating this plan. Relevant overlap exists in `20260712231612_CompleteDemoSeed.cs`, `20260724190000_AddUsPortfolioDemoSeed.cs`, `PadelitoDbContextModelSnapshot.cs`, `PadelitoDbContext.cs`, `Program.cs`, and migration/payment tests.
- Plan date: 2026-07-27 (America/Buenos_Aires)
- Reproducibility warning: the audit report and this plan are untracked, and the evidence includes substantial pre-existing working-tree changes. In particular, `AddUsPortfolioDemoSeed.cs` has a material 24-addition/21-deletion user diff. Implementation must preserve those changes or obtain an explicit decision that the demo migration is to be neutralized despite them.

## Evidence inspected

### Facts

- `docs/production-readiness.md:55-68` defines `PRD-BLK-002` as `OPEN`.
- `backend/Padelito.Infrastructure/Data/Migrations/20260708144132_InitialCreate.cs:359-421` inserts an application-owned Club, Person, Employee, and administrator User in addition to global reference catalogs.
- `backend/Padelito.Infrastructure/Data/Migrations/20260708190000_SetAdminDemoPassword.cs:14-29` changes the seeded demo administrator password.
- `backend/Padelito.Infrastructure/Data/Migrations/20260712231612_CompleteDemoSeed.cs:14-100` inserts demo courts, people, clients, promotions, turns, reservations, payments, and audits; its `Down` method deletes those rows.
- `backend/Padelito.Infrastructure/Data/Migrations/20260720184546_RequirePersonContactFields.cs:13-24` performs a data precondition check before making contact columns required. It is schema/data-integrity logic, not demo provisioning, and must remain.
- `backend/Padelito.Infrastructure/Data/Migrations/20260721232908_PrepareProductionData.cs:16-194` deletes the seeded business graph, including Club `1` and User `1`; its `Down` method recreates the demo graph.
- `backend/Padelito.Infrastructure/Data/Migrations/20260724160000_LocalizeDemoDataForUs.cs:15-88` updates global reference values and creates the available-turn overlap trigger. It does not create a Club or administrator.
- `backend/Padelito.Infrastructure/Data/Migrations/20260724190000_AddUsPortfolioDemoSeed.cs:17-27` selects an active Club and User/Employee and throws SQL error `51010` if either is absent.
- The same migration, lines 29-105, unconditionally inserts demo courts, people, clients, promotions, turns, reservations, payments, and audits into the selected Club; lines 109-121 delete fixed ID ranges during rollback.
- Migration filename/ID order places `AddUsPortfolioDemoSeed` after `PrepareProductionData` and `LocalizeDemoDataForUs`, so a clean chain reaches it with no Club or User and aborts.
- `backend/Padelito.Api/Program.cs:93-95` builds the application and then invokes `ProductionBootstrapper.InitializeAsync`. It does not invoke `Database.Migrate`.
- `backend/Padelito.Api/Startup/ProductionBootstrapper.cs:17-77` is an opt-in, transactional bootstrap that creates a Club, Person, Employee, and hashed administrator after the schema exists. It cannot satisfy a prerequisite inside an earlier EF migration.
- `ProductionBootstrapper.InitializeAsync` returns when any User exists, but it checks neither whether other application-owned tables are empty before first bootstrap nor whether an existing graph matches the configured bootstrap identity.
- `backend/Padelito.Application.Tests/ProductionBootstrapperTests.cs:51-73` uses EF InMemory and `EnsureCreated`; it does not execute the SQL Server migration chain or prove SQL migration/bootstrap behavior.
- `backend/Padelito.Infrastructure/Data/Seed/PadelitoSeedData.cs:8-32` currently limits model-managed seed data to global reference catalogs: Roles, ReservationStatuses, PaymentMethods, and CourtTypes.
- `README.md:208-218` instructs users to apply migrations and says that migrations load a complete demo dataset. That contract is unsafe for production and conflicts with explicit bootstrap.
- On 2026-07-27, the user ran a read-only query against the MonsterASP.NET persistent database and reported these `__EFMigrationsHistory` rows:
  - `20260708144132_InitialCreate`
  - `20260708190000_SetAdminDemoPassword`
  - `20260710150950_EnableCancelledSlotRebooking`
  - `20260712231612_CompleteDemoSeed`
  - `20260723010000_PreventOverlappingAvailableTurns`
- Relative to the current repository, that reported history does not include `20260720184546_RequirePersonContactFields`, `20260721232908_PrepareProductionData`, `20260724160000_LocalizeDemoDataForUs`, or `20260724190000_AddUsPortfolioDemoSeed`. The destructive cleanup and failing portfolio seed are therefore not recorded as applied in that environment.
- The user classified that database as demo/portfolio-only with no real customer, reservation, payment, or audit data.
- No migration or database command was run in plan mode. No production, remote, or local database was accessed.

### Inferences

- On a genuinely empty SQL Server database, the current chain will fail at `20260724190000_AddUsPortfolioDemoSeed` before application bootstrap can run.
- A database that had an active Club/User at that migration boundary can receive fabricated business and financial records.
- The user-reported MonsterASP.NET history proves that historical demo migrations were applied there, while `PrepareProductionData` and `AddUsPortfolioDemoSeed` remain pending. Running `database update` from the current unsafe working tree could therefore delete the existing demo Club graph and later fail or add another fabricated dataset.
- EF will not rerun migration IDs already present in `__EFMigrationsHistory`. Neutralizing historical bodies therefore protects clean installs and pending migrations without modifying rows created by already-recorded migrations.
- Retaining the historical migration IDs while removing only application/demo data operations is more compatible than deleting/squashing the migration chain: already-applied databases retain their history, while clean and not-yet-upgraded databases execute deterministic schema/reference operations.

## Root cause and impact

- Root cause: production schema evolution and environment-specific demo provisioning were mixed into the same EF migration chain. A cleanup migration removed the prerequisite business identity, a later demo migration required that identity, and bootstrap was sequenced after migrations.
- Affected behavior: clean database creation; upgrades that cross any demo-data migration boundary; first-administrator provisioning; rollback across demo migrations; any database into which IDs `1`, `9001-9007`, or `9101-9108` were inserted or later reused.
- Security/production impact: deployment can fail partway through, predictable demo credentials/history can enter a persistent database, and fabricated customer/payment/audit data can be mistaken for real records. Fixed-ID rollback deletes are unsafe if those IDs have been repurposed.
- System boundaries: EF Core migration assembly and history; SQL Server schema/reference data; API startup/bootstrap; deployment documentation; migration and bootstrap tests; operator-owned persistent databases.
- Entity classification:
  - installation-owned: Club, Person, Employee, User, Client, Court, Promotion, AvailableTurn, Reservation, Payment, ReservationAudit;
  - global reference data: Role, ReservationStatus, PaymentMethod, CourtType;
  - migration history: installation metadata, not business data.

## Proposed remediation

### Solution

Subject to the human decisions below:

1. Retain all existing migration IDs/classes so databases with matching `__EFMigrationsHistory` remain compatible.
2. Remove application/demo data mutations from the deployable migration chain:
   - in `InitialCreate`, retain schema plus global reference inserts, but remove the Club/Person/Employee/User inserts;
   - make `SetAdminDemoPassword` a documented no-op;
   - make `CompleteDemoSeed` a documented no-op in both directions;
   - make `PrepareProductionData` a documented no-op in both directions rather than deleting/recreating business and financial rows;
   - make `AddUsPortfolioDemoSeed` a documented no-op in both directions, removing both its prerequisite throw and all fixed-ID inserts/deletes.
3. Keep `RequirePersonContactFields` schema/precondition logic, `PreventOverlappingAvailableTurns`, `LocalizeDemoDataForUs` global-reference/trigger logic, and unrelated schema migrations intact.
4. Keep model seed configuration restricted to the four global reference catalogs and add a regression assertion that the model has no `HasData` entries for application-owned entities.
5. Harden the existing bootstrap state machine:
   - disabled means no reads/writes beyond normal startup;
   - a completely unprovisioned application database may create exactly one Club/admin graph transactionally;
   - a completed graph matching the configured bootstrap identity is an idempotent no-op;
   - any partially populated, ambiguous, mismatched, or additional application-owned state fails closed without writes;
   - logging records outcome and identifiers suitable for audit, but never logs the password or secret values.
6. Document the release sequence explicitly: apply reviewed schema migrations first, run the API once with bootstrap enabled and protected configuration, verify the single Club/admin graph, then disable bootstrap and remove its password from the runtime secret source.
7. Add a read-only SQL preflight artifact that reports:
   - applied migration IDs;
   - counts and monetary totals for all application-owned tables;
   - exact and fingerprint matches for known seed IDs/names/emails/audit text;
   - relationships or post-seed activity attached to suspected rows.
   The script must not update or delete anything and must not label a row “demo” based on ID alone.
8. Do not automatically clean an existing database. If the preflight finds suspected seed data, preserve it and create a separate, approved reconciliation plan with business ownership, row-count/monetary reconciliation, backup/restore evidence, and explicit disposition rules.

### Alternatives considered

- Delete/squash all migrations and generate a new initial migration: cleanest only if no persistent database has ever applied the chain, but it breaks migration-history continuity and is not approved without deployment inventory.
- Add a later migration that deletes demo rows: rejected because the clean chain fails before reaching it and automatic deletion could destroy real financial/history data.
- Make `AddUsPortfolioDemoSeed` conditional on an existing Club/User: rejected because it still inserts fabricated data into eligible databases.
- Run bootstrap before migrations or call `Database.Migrate` from API startup: rejected because bootstrap requires schema/reference tables and application startup should not silently perform production schema changes.
- Leave earlier demo inserts followed by `PrepareProductionData`: rejected because production migration steps would still create/delete application and financial data and partial upgrades would remain unsafe.

### Files expected to change

- `backend/Padelito.Infrastructure/Data/Migrations/20260708144132_InitialCreate.cs`
- `backend/Padelito.Infrastructure/Data/Migrations/20260708190000_SetAdminDemoPassword.cs`
- `backend/Padelito.Infrastructure/Data/Migrations/20260712231612_CompleteDemoSeed.cs`
- `backend/Padelito.Infrastructure/Data/Migrations/20260721232908_PrepareProductionData.cs`
- `backend/Padelito.Infrastructure/Data/Migrations/20260724190000_AddUsPortfolioDemoSeed.cs`
- `backend/Padelito.Api/Startup/ProductionBootstrapper.cs`
- `backend/Padelito.Application.Tests/ProductionBootstrapperTests.cs`
- a focused SQL Server migration-chain integration test under `backend/Padelito.Application.Tests/`
- `backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj` only if a narrowly scoped test dependency is required
- `docs/remediation/sql/PRD-BLK-002-preflight.sql`
- `README.md`
- this plan's implementation and verification sections

Historical designer files and `PadelitoDbContextModelSnapshot.cs` are not expected to change solely to neutralize migration bodies; the current snapshot already contains only global `HasData`. Any generated snapshot drift must be reviewed and justified rather than accepted mechanically.

### Explicit implementation boundaries

- Do not apply any migration or preflight to production, remote, shared, or non-disposable data.
- Do not delete, rewrite, archive, or “correct” existing business/financial records.
- Do not invent a rule that fixed IDs, names, or emails alone prove a row is disposable demo data.
- Do not expose, rotate, or commit bootstrap credentials.
- Do not add automatic API-startup migration application.
- Do not change schema, payment invariants, tenant authorization, or `PRD-BLK-003`.
- Do not change global reference catalog semantics except as already required by the current model.
- Preserve unrelated user changes. Stop implementation if the relevant migration diffs cannot be isolated or the user does not authorize superseding the current `AddUsPortfolioDemoSeed` content.

### Adjacent/out-of-scope findings

- `PRD-BLK-003` owns the untracked destructive `EnforceFullReservationPayment` migration and must be planned separately.
- `PRD-BLK-001` owns the broader single-club authorization/readiness invariant. This plan changes bootstrap only enough to make first provisioning explicit and fail closed.
- CI/CD, backup infrastructure, release automation, general observability, and final application readiness remain outside this finding.
- Any cleanup of suspected demo rows in a persistent database requires a separate data-reconciliation plan.

## Compatibility and data

- Data-model implications: none intended. Application-owned tables start empty; only global reference catalogs are seeded by migrations.
- Migration implications: historical IDs remain unchanged, but selected historical `Up`/`Down` bodies no longer mutate application/demo data. This deliberate amendment requires approval because deployment history is unknown.
- Clean-install implications: the complete chain must finish with zero application-owned rows, no demo prerequisite, and only schema/global reference data. Bootstrap then creates the first Club/admin graph.
- Upgrade implications:
  - migrations already recorded as applied are not rerun and their existing data is untouched;
  - migrations not yet applied execute the amended deterministic bodies;
  - the reported MonsterASP.NET database must not receive a migration update until the amended chain is implemented, scripted, reviewed, and rehearsed;
  - databases paused before `PrepareProductionData` may still contain old demo rows inserted by previously applied code, so preflight/reconciliation is mandatory before upgrade;
  - a database where `AddUsPortfolioDemoSeed` already ran may contain `910x` data and requires preflight, not automatic cleanup.
- Compatibility implications: preserving migration IDs avoids an `__EFMigrationsHistory` fork. Generated idempotent SQL must be reviewed to ensure no application-owned `INSERT`, `DELETE`, demo credential hash, demo literal, or prerequisite `THROW 51010` remains.
- Rollback/roll-forward/recovery strategy:
  - roll forward with the amended chain; never use rollback as a demo cleanup mechanism;
  - crossing a now-no-op migration in `Down` must leave application-owned data untouched;
  - if rehearsal fails, discard only the explicitly disposable test database and correct code/scripts;
  - for persistent data, stop, preserve evidence, and restore only under an operator-approved backup/restore procedure.
- Backup or reconciliation requirements: inventory every persistent environment before implementation approval. Before any real upgrade, capture/test a restorable backup and run the read-only preflight. Suspected data requires row counts, payment totals, relationship tracing, and an explicit human disposition.

## Testing and verification

### Regression tests

- Static migration test: inspect migration operations or generated idempotent SQL and prove application-owned entities are never inserted/deleted by the production chain.
- Model metadata test: only Role, ReservationStatus, PaymentMethod, and CourtType carry seed data.
- Bootstrap unit tests:
  - disabled bootstrap creates nothing;
  - complete empty application state creates exactly one Club, Person, Employee, and Admin User;
  - second identical invocation is a no-op;
  - existing Club without User, existing User graph that does not match configuration, partial graph, or multiple application roots throws and leaves all counts unchanged;
  - missing/weak configuration fails before writes;
  - password is hashed and no secret appears in captured logs.
- SQL Server migration-chain integration:
  - from an empty disposable database, apply the complete chain and assert global reference counts plus zero rows in every application-owned table;
  - bootstrap, assert the exact single graph and zero demo business rows, invoke again, and assert no changes;
  - apply through `20260724160000_LocalizeDemoDataForUs`, bootstrap a legitimate graph, then migrate to latest and prove no portfolio rows are added;
  - generate an idempotent script and execute it twice against a fresh disposable database, proving both runs succeed and final counts/totals are stable.

EF InMemory may cover configuration/control flow, but it is not sufficient evidence for migration SQL, transactions, migration history, or SQL Server behavior.

### Negative/bypass cases

- No active Club/User exists at the former `AddUsPortfolioDemoSeed` boundary.
- A legitimate Club/User already exists at that boundary.
- Known `900x`/`910x` IDs are already occupied by non-demo data.
- A migration is already recorded as applied.
- Bootstrap is accidentally left enabled after successful provisioning.
- Bootstrap sees partial or mismatched state.
- `Down` crosses neutralized demo migrations and does not delete application-owned rows.
- The preflight encounters fingerprint ambiguity; it reports evidence without classifying or changing the row.

### Verification commands

Run from the repository root after implementation and record exact exit codes/results. Generate artifacts only in a disposable temp directory.

```powershell
git status --short --branch
git diff --check
git diff -- backend/Padelito.Infrastructure/Data/Migrations backend/Padelito.Api/Startup/ProductionBootstrapper.cs backend/Padelito.Application.Tests README.md docs/remediation
dotnet build backend/Padelito.Api/Padelito.Api.csproj --no-restore
dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductionBootstrapperTests|FullyQualifiedName~MigrationChain"
dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj --no-restore
dotnet tool run dotnet-ef migrations list --no-build --project backend/Padelito.Infrastructure --startup-project backend/Padelito.Api
dotnet tool run dotnet-ef migrations script --idempotent --no-build --project backend/Padelito.Infrastructure --startup-project backend/Padelito.Api --output <DISPOSABLE_TEMP_PATH>\PRD-BLK-002-idempotent.sql
rg -n -i "AddUsPortfolioDemoSeed|THROW 51010|admin@padelito|carlos|luc[ií]a|maya|ethan|sofia|liam|ava|demo|portfolio|INSERT INTO \[(Clubs|People|Employees|Users|Clients|Courts|Promotions|AvailableTurns|Reservations|Payments|ReservationAudits)\]|DELETE FROM \[(Clubs|People|Employees|Users|Clients|Courts|Promotions|AvailableTurns|Reservations|Payments|ReservationAudits)\]" <DISPOSABLE_TEMP_PATH>\PRD-BLK-002-idempotent.sql
```

The SQL Server integration test command must be run only after its harness proves that the target server is local/test-only and that the generated database name is unique and disposable. It must refuse non-local or non-test connection settings and must never accept production credentials. The exact environment-specific command and database evidence must be recorded during verification.

### Required environment

- .NET 10 SDK, restored locked dependencies, and restored local `dotnet-ef` tool.
- An explicitly disposable, empty SQL Server database/server controlled by the test harness; no remote/shared/real data.
- An optional anonymized production-like snapshot supplied and approved by the operator for read-only/preflight rehearsal. If this is unavailable, do not claim that existing-data safety was fully verified.
- No production secrets; bootstrap test credentials must be ephemeral.

### Evidence required for resolution

- Reviewed idempotent SQL contains schema/global reference operations but no application/demo inserts/deletes, demo password mutation, or `THROW 51010`.
- Empty SQL Server chain succeeds and finishes with the expected zero application-owned row counts.
- Bootstrap succeeds only in the allowed empty state, is idempotent for its exact completed graph, and fails closed without writes for ambiguous states.
- Existing-legitimate-Club rehearsal receives no demo rows.
- Persistent-environment migration inventory is recorded, and every database that could have run old demo migrations has either clean preflight evidence or a separately approved reconciliation outcome.
- Focused and full backend build/tests pass.
- Current relevant diffs match this approved scope and preserve unrelated work.

## Delivery controls

### Risks

- Historical migrations may already be deployed; amending their bodies without inventory can create different behavior for databases at different history points.
- Current user changes materially overlap `AddUsPortfolioDemoSeed.cs`; a mechanical replacement would discard intentional edits.
- Previously inserted demo rows may have been edited, referenced, or mistaken for real data, so automated fingerprint cleanup is unsafe.
- A bootstrap state check that is too weak can create a second Club/admin; one that is too broad can block legitimate recovery.
- SQL Server/Docker availability may prevent required integration evidence; the finding must remain `OPEN` or `PARTIALLY RESOLVED` until that evidence exists.
- `PRD-BLK-003` can independently make the latest chain unsafe even after this finding is implemented.

### Dependencies

- Human-confirmed migration deployment inventory.
- Explicit approval of the historical-migration compatibility strategy.
- Resolution of the overlapping `AddUsPortfolioDemoSeed.cs` user diff.
- Disposable SQL Server capability for verification.
- Separate `PRD-BLK-003` remediation before an overall migration chain can be called production-safe.

### Assumptions

- The supported production model is one exclusive database per club installation, consistent with the approved `PRD-BLK-001` plan.
- Roles, reservation statuses, payment methods, and court types are stable global reference catalogs.
- Application-owned business data must be absent after schema migration and created only through explicit bootstrap/application workflows.
- Existing user changes are intentional and must be preserved.
- The only known persistent environment is the user-reported MonsterASP.NET demo/portfolio database. No database contents, row counts, monetary totals, or secret values were inspected by local automation.

### Decisions requiring human input

None remain. Decisions recorded from the user on 2026-07-27:

1. The known MonsterASP.NET database is demo/portfolio-only and contains no real customer, reservation, payment, or audit data.
2. Preserve every historical migration ID/class and neutralize all application/demo data operations in the five migrations listed by this plan.
3. The current working-tree edits to `AddUsPortfolioDemoSeed.cs` may be superseded by converting that migration to a no-op; no part of its demo dataset needs preservation in the production migration chain.
4. Do not clean, delete, migrate, or otherwise modify the remote MonsterASP.NET database automatically.
5. Existing remote demo rows remain out of scope. Any later retirement, rebuild, or replacement of that database is an explicit operator action outside this remediation.

These decisions authorize local implementation of the plan only. They do not authorize remote migration application, deployment, production access, or data deletion.

### Definition of done

`PRD-BLK-002` may be marked `RESOLVED` only when:

1. The human decisions above are recorded.
2. The deployable EF chain contains schema and global reference data only.
3. Clean SQL Server provisioning succeeds without a Club/User prerequisite and leaves all application-owned tables empty.
4. Explicit bootstrap creates exactly one valid Club/admin graph, is safely idempotent, and fails closed on ambiguous state.
5. An existing legitimate Club/admin receives no fabricated rows when upgraded.
6. The idempotent migration script has been reviewed and contains no demo application-data mutations or demo credential operation.
7. Persistent migration histories have been inventoried; possible legacy demo data has clean preflight evidence or a completed, separately approved reconciliation.
8. Required focused/full tests and SQL Server rehearsals pass with recorded evidence.
9. Documentation describes the migration/bootstrap/disable sequence and no longer presents production migrations as a demo loader.
10. No unrelated user changes were overwritten, no real database was mutated during remediation, and `PRD-BLK-003` remains separately tracked.

## Implementation record

Implemented locally on 2026-07-27 against branch `main`, commit snapshot
`7986f1df158c9c252f093e7a434d3aa113df7981`, with the pre-existing dirty
working tree preserved.

### Changes

- Removed application-owned Club/Person/Employee/User seed operations from
  `20260708144132_InitialCreate`; retained only global reference seed operations.
- Retained the historical IDs/classes for `SetAdminDemoPassword`,
  `CompleteDemoSeed`, `PrepareProductionData`, and `AddUsPortfolioDemoSeed`, but
  converted their `Up`/`Down` application/demo mutations to documented no-ops.
- Hardened `ProductionBootstrapper` with a serializable transaction:
  - exact completed bootstrap state is idempotent;
  - partial, mismatched, additional, or ambiguous application data fails closed;
  - disabled bootstrap performs no bootstrap work;
  - no password or secret value is logged.
- Expanded `ProductionBootstrapperTests` to cover creation/idempotency, disabled
  operation, weak configuration, partial state, mismatched state, migration
  operations, and model seed boundaries.
- Added `docs/remediation/sql/PRD-BLK-002-preflight.sql`, a read-only inventory,
  count, monetary-total, fingerprint, relationship, and audit-evidence script.
- Updated `README.md` to state that migrations provision schema/global reference
  data only and to document the explicit bootstrap/disable sequence.

### Validation run during implementation

- Initial `dotnet build backend/Padelito.Api/Padelito.Api.csproj --no-restore`:
  failed with two compile errors because `SetAdminDemoPassword.Designer.cs`
  already supplies the migration attributes. The migration class was corrected
  to remain `partial` without duplicate attributes.
- Repeated API build: exit `0`; 0 warnings, 0 errors. The SDK emitted only its
  informational preview-version message.
- Focused tests after final test changes:
  `dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductionBootstrapperTests"`:
  exit `0`; 7 passed, 0 failed, 0 skipped.
- Full backend tests:
  `dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj --no-restore`:
  exit `0`; 63 passed, 0 failed, 0 skipped.
- Generated a temporary idempotent script with `dotnet-ef migrations script
  --idempotent --no-build`; exit `0`, 30,465 bytes. The script contained all
  migration IDs through `20260724201719_EnforceFullReservationPayment`.
- Static script review found:
  - no `THROW 51010`;
  - no known demo identities, emails, credentials, or portfolio literals;
  - no PRD-BLK-002 application-owned `INSERT` or demo-data `DELETE`
    statements; verification later confirmed that the separate
    `PRD-BLK-003` migration still contains its tracked payment deletion;
  - the four neutralized migration IDs only insert their
    `__EFMigrationsHistory` rows.
- `git diff --check`: exit `0`.

### Deviations and remaining implementation evidence

- No SQL Server migration-chain integration test was added or executed because
  no explicitly disposable local SQL Server target was established. EF InMemory
  is used only for bootstrap control-flow tests and is not claimed as SQL Server
  proof.
- The preflight script was not executed against the remote MonsterASP.NET
  database. The user explicitly prohibited automatic remote modification, and
  this skill prohibits remote/production access.
- No migration was applied, no database was created/dropped, no deployment was
  performed, and no secret was read or changed.
- SQL Server clean-install, upgrade, repeated-idempotent-script, and bootstrap
  transaction evidence remains mandatory in `verify` mode before the finding
  can be marked resolved.

## Verification evidence

- Verification date: 2026-07-27 (America/Buenos_Aires)
- Branch: `main`
- Commit or working-tree state: commit
  `7986f1df158c9c252f093e7a434d3aa113df7981`; dirty working tree with the
  implementation and unrelated pre-existing changes preserved
- Finding: `PRD-BLK-002`
- Approved scope: preserve historical migration IDs, neutralize demo/application
  data operations, make `AddUsPortfolioDemoSeed` a no-op, harden explicit
  bootstrap, add regression coverage and documentation, and never modify or
  clean the remote MonsterASP.NET database
- Diff reviewed: full repository diff plus every planned migration, bootstrap,
  test, README, plan, and preflight change
- Scope deviations: no material implementation scope drift. The required
  repeated idempotent-script rehearsal exposed an existing trigger-script
  incompatibility described below. No application source was changed in verify
  mode.

### Root-cause verification

- Property verified: production migrations no longer provision, delete, or
  require demo/application-owned data, and explicit bootstrap is the only path
  that creates the first Club/administrator graph.
- Method:
  - applied the complete chain to a new disposable SQL Server LocalDB database;
  - applied through `20260724160000_LocalizeDemoDataForUs`, ran the real API
    bootstrap twice, then migrated to latest;
  - generated the idempotent SQL, reviewed its relevant operations, and attempted
    to execute it twice on another new disposable database;
  - ran focused and full automated tests and reviewed the implementation diff.
- Result:
  - complete clean installation applied all 10 migration IDs and retained only
    global reference catalogs: Roles `3`, ReservationStatuses `4`,
    PaymentMethods `5`, CourtTypes `4`;
  - all application-owned tables were empty after clean migration;
  - first bootstrap produced exactly Clubs/People/Employees/Users `1/1/1/1`,
    with Clients/Reservations/Payments `0/0/0`;
  - the second identical bootstrap kept exactly the same counts;
  - migrating that legitimate graph from the localization boundary to latest
    kept `1/1/1/1` and left Clients/Courts/AvailableTurns/Promotions/
    Reservations/Payments/ReservationAudits all at `0`;
  - the generated idempotent script contained no application-owned `INSERT`,
    no known demo identity or credential, and no `THROW 51010`;
  - the script did not complete its first execution: SQL Server 2019 returned
    error `156`, `Incorrect syntax near the keyword 'OR'`, at the
    `CREATE OR ALTER TRIGGER` emitted inside EF's idempotent `IF` wrapper.
- Bypasses/incomplete paths checked: no Club/User at the former portfolio-seed
  boundary; a legitimate Club/User at that boundary; repeated bootstrap;
  partial and mismatched bootstrap states; disabled/invalid bootstrap; migration
  operation and model-seed boundaries; already-applied migration handling via
  EF history and the idempotent script review.
- Remaining limitations:
  - the required idempotent script cannot currently be executed once, therefore
    cannot be proven stable on a second execution;
  - the remote MonsterASP.NET demo database was not accessed or changed, as
    required by the approved boundary;
  - `PRD-BLK-003` still contributes a separate payment deletion to the latest
    migration chain and remains independently blocking.

### Tests and commands

| Command | Purpose | Result | Exact evidence |
|---|---|---|---|
| `dotnet build backend/Padelito.Api/Padelito.Api.csproj --no-restore` | Compile API and dependencies | `PASS` | exit `0`; 0 warnings; 0 errors; SDK preview informational message only |
| `dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj --no-restore --filter "FullyQualifiedName~ProductionBootstrapperTests\|FullyQualifiedName~MigrationChain"` | Focused bootstrap/migration regression suite | `PASS` | exit `0`; 7 passed, 0 failed, 0 skipped |
| `dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj --no-restore` | Full backend regression suite | `PASS` | exit `0`; 63 passed, 0 failed, 0 skipped |
| `dotnet-ef database update` against a new disposable LocalDB database | Clean migration-chain rehearsal | `PASS` | 10 migrations; global catalogs `3/4/5/4`; every application-owned table `0` |
| migrate through `20260724160000_LocalizeDemoDataForUs`, start API bootstrap twice, then migrate to latest | Upgrade, bootstrap, and idempotency rehearsal | `PASS` | first `1/1/1/1`; second `1/1/1/1`; latest preserves graph and adds zero business/demo rows |
| `dotnet-ef migrations script --idempotent` | Generate deployment SQL | `PASS` | exit `0`; 30,465 bytes; 10 migration-history inserts |
| `sqlcmd -b -I -i <temporary-idempotent-script>` on a new disposable LocalDB database | Required two-run idempotent rehearsal | `FAIL` | first run exits `1`; SQL Server error `156`: incorrect syntax near `OR` in `CREATE OR ALTER TRIGGER`; second run not reachable |
| generated-script static scans | Detect PRD-BLK-002 demo mutations/bypass | `PASS` | application-owned inserts `0`; known demo identities/credential/`THROW 51010` `0`; separate PRD-BLK-003 payment delete `1` |
| `git diff --check` | Patch whitespace integrity | `PASS` | exit `0` |

Every disposable database created for verification was dropped in a `finally`
cleanup path. Temporary generated SQL files were removed. No remote database,
deployment, credential store, or real data was accessed or modified.

### Change evidence

- Summary: historical IDs are preserved; five demo/application-data migration
  bodies are neutralized as approved; bootstrap is transactional, exact, and
  fail-closed; production documentation no longer advertises demo provisioning.
- Files changed: the five scoped migration files,
  `ProductionBootstrapper.cs`, `ProductionBootstrapperTests.cs`, `README.md`,
  and the read-only preflight/plan evidence.
- Tests added/updated: seven focused bootstrap/migration/model-seed tests.
- Unrelated behavior checked: full backend suite passes. Unrelated dirty changes
  were not overwritten.

### Status decision

- Previous status: `OPEN`
- New status: `PARTIALLY RESOLVED`
- Evidence supporting status: the original demo-data prerequisite and fabricated
  provisioning root cause is corrected and proven against real disposable SQL
  Server databases; bootstrap and upgrade behavior pass.
- Unresolved portions: the approved definition of done explicitly requires the
  generated idempotent migration script to execute twice. It currently fails on
  the trigger DDL before completing its first run. Persistent legacy-demo
  evidence also remains operator-owned and the remote database remains
  intentionally untouched.
- Recommended next command:
  `/production-remediation plan PRD-BLK-002` to scope the idempotent trigger-DDL
  correction, then `/production-remediation implement PRD-BLK-002` and rerun
  this verification. `PRD-BLK-003` must still be remediated separately before
  the complete chain can be considered production-safe.
