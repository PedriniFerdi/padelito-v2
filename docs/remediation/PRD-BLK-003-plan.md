# Remediation plan: PRD-BLK-003 — A migration deletes financial records to enforce the new payment model

## Snapshot

- Result: `CONFIRMED`
- Finding status: `OPEN`
- Branch: `main`
- Commit: `7986f1df158c9c252f093e7a434d3aa113df7981`
- Working tree: dirty. The readiness report, the cited migration, its designer, and the existing remediation directory are untracked; payment API, service, repository, DTO, EF model/snapshot, frontend, tests, and other migrations also contain local changes.
- Plan date: `2026-07-27`
- Reproducibility warning: this finding was revalidated against the current working tree, not against a clean commit. `HEAD` still implements partial payments and does not contain `20260724201719_EnforceFullReservationPayment`; therefore the evidence and line references below cannot be reproduced from `HEAD` alone. Preserve all current user changes and revalidate the relevant diff before implementation.

## Evidence inspected

### Original finding

- `docs/production-readiness.md:76-85` records `PRD-BLK-003` as `OPEN`.
- The report states that the pending migration deletes payments for reservations whose payment count or sum does not match `FinalPrice`, without archival, reconciliation output, an approval gate, or a backup/restore plan.

### Current facts

- `backend/Padelito.Infrastructure/Data/Migrations/20260724201719_EnforceFullReservationPayment.cs:13-25` executes a set-based `DELETE` against `Payments`.
- The deletion selects every reservation that has payment rows and for which either:
  - `COUNT(*) <> 1`; or
  - `SUM(Amount) <> Reservations.FinalPrice`.
- Consequently, the migration deletes all payment rows for:
  - partial-payment histories;
  - multiple payments whose total exactly equals the reservation price;
  - duplicate payments;
  - overpayments;
  - adjusted or otherwise historically inconsistent payment histories.
- `backend/Padelito.Infrastructure/Data/Migrations/20260724201719_EnforceFullReservationPayment.cs:27-35` drops the non-unique reservation index and creates a unique index only after the deletion.
- `backend/Padelito.Infrastructure/Data/Migrations/20260724201719_EnforceFullReservationPayment.cs:39-48` reverses only the index. It cannot reconstruct deleted financial records.
- `backend/Padelito.Infrastructure/Data/PadelitoDbContext.cs:273-291` and `backend/Padelito.Infrastructure/Data/Migrations/PadelitoDbContextModelSnapshot.cs:225-252` model one payment at most per reservation through a unique `ReservationId` index.
- `backend/Padelito.Infrastructure/Repositories/PaymentRepository.cs:39-100` implements the new runtime rule transactionally:
  - retrieves the club-owned reservation;
  - permits only active reservations before slot start;
  - rejects a reservation that already has any payment;
  - records exactly `Reservation.FinalPrice`;
  - automatically confirms a pending reservation and writes an audit entry.
- `backend/Padelito.Application/Services/PaymentService.cs:26-46` no longer accepts an amount or payment date from the caller.
- `backend/Padelito.Application.Tests/PaymentServiceTests.cs` covers server-calculated full payment, automatic confirmation, second-payment rejection, and time-bound rejection, but these are service/fake-repository tests. They do not prove SQL Server migration safety, row preservation, monetary reconciliation, or unique-index behavior.
- `backend/Padelito.Application.Tests/ApiIntegrationTests.cs:141-155` rejects client-supplied amount/date, but the API test host does not establish the required existing-data SQL Server migration property.
- The original schema in `backend/Padelito.Infrastructure/Data/Migrations/20260708144132_InitialCreate.cs:307-336,450-458` permits multiple payments per reservation and enforces only positive payment amounts and foreign keys. Existing databases can therefore legitimately contain records that the new migration classifies as invalid.
- `README.md:140-141,185` still describes partial-payment/concurrent-balance behavior, while the current working-tree runtime implements one full payment. This documentation drift is adjacent evidence of a behavioral compatibility change; correcting the general product documentation is outside the destructive-migration fix unless needed to document the release procedure.
- `docs/remediation/PRD-BLK-002-plan.md` records that the generated migration chain includes this migration and that a static scan found one separate payment deletion attributable to `PRD-BLK-003`.
- No repository artifact currently provides a PRD-BLK-003 exception report, reconciliation rules, immutable archive, operator approval record, or tested backup/restore procedure.
- No production, remote, shared, or persistent database was accessed. Applied migration history and actual exception counts in persistent environments are unknown.

### Relevant Git evidence

- Branch: `main`.
- Commit: `7986f1df158c9c252f093e7a434d3aa113df7981`.
- The working tree contains 44 modified tracked files plus relevant untracked migration/test/remediation files.
- Relevant overlap includes:
  - payment DTO/service/repository/API/frontend behavior;
  - `PadelitoDbContext` and its snapshot;
  - payment and API tests;
  - migration-chain changes made for `PRD-BLK-002`;
  - the untracked `EnforceFullReservationPayment` migration and designer.
- Implementation must not overwrite, normalize, or silently absorb unrelated work. Because the migration and report are untracked, the implementation snapshot must include untracked files explicitly.

### Inferences

- The untracked source and report wording strongly suggest the migration is pending in this repository, but they do not prove that the same migration ID has never been applied to a persistent database.
- The runtime change establishes the intended rule for new payments. It does not define how historical partial, multiple, duplicate, adjusted, refunded, or overpaid records should be represented.
- A unique reservation index can enforce “at most one payment,” but it cannot enforce that `Payment.Amount == Reservation.FinalPrice`; that cross-table invariant remains an application/service responsibility unless a separately designed database mechanism is approved.

## Root cause and impact

- Root cause: a cardinality change from many payments per reservation to at most one full payment was encoded as a destructive schema migration. The migration treats every incompatible historical state as disposable instead of treating it as an exception requiring inventory and an approved business disposition.
- Affected behavior:
  - upgrades of any database containing legacy partial, multiple, adjusted, duplicate, or overpaid histories;
  - generated deployment scripts containing the migration;
  - rollback, because `Down` cannot restore deleted rows;
  - reports, balances, reservation history, and audit interpretation after deleted payments disappear.
- Security/production impact: irreversible loss of financial evidence, changed monetary totals, broken auditability, and an inability to demonstrate that an upgrade preserved legitimate history.
- System boundaries:
  - EF Core model and migration metadata;
  - SQL Server `Payments`, `Reservations`, `PaymentMethods`, foreign keys, index, and `__EFMigrationsHistory`;
  - application payment creation and reporting behavior;
  - operator migration/reconciliation/backup workflow;
  - regression tests against a real disposable SQL Server.
- Entity classification:
  - `Payment`, `Reservation`, and `ReservationAudit`: club-owned financial/operational history, reached through `Reservation -> AvailableTurn -> Court -> Club`;
  - `PaymentMethod`: installation-global reference data;
  - payment history ownership is not ambiguous, but the business meaning and disposition of incompatible historical records is ambiguous and requires human approval.

## Proposed remediation

### Solution

1. Inventory every persistent environment's `__EFMigrationsHistory` before approving implementation:
   - confirm whether `20260724201719_EnforceFullReservationPayment` has run anywhere;
   - record the environment owner and migration boundary without reading or exposing secrets;
   - do not run an update while this finding remains open.
2. Add `docs/remediation/sql/PRD-BLK-003-preflight.sql` as a read-only SQL Server report. It must:
   - refuse to mutate data;
   - report the current migration-history state;
   - report total payment row count and `SUM(Amount)`;
   - classify reservations into zero payments, one exact full payment, one non-matching payment, and multiple payments;
   - list exception identifiers, club/reservation IDs, payment count, reservation `FinalPrice`, payment total, and variance;
   - list the constituent payment IDs, methods, dates, amounts, and notes needed for review, avoiding unrelated personal data;
   - make no claim that an exception is erroneous or safe to merge/delete.
3. Remove all destructive payment DML from the deployable schema transition.
4. Select the compatible migration path from the recorded history:
   - if the migration ID has never been applied to any persistent/shared environment, amend the pending `EnforceFullReservationPayment.Up` in place so it checks for one or more payments that violate the new cardinality/amount rule and throws a clear operator-facing error before dropping the old index;
   - if the ID has been applied anywhere persistent/shared, do not rewrite it. Preserve migration history and create a new additive corrective migration/runbook after analyzing the affected database and the already-lost-data recovery path.
5. In the not-yet-applied path, keep reservations with zero payments valid. The guard applies only when payment rows exist:
   - one payment equal to `FinalPrice`: compatible;
   - one payment unequal to `FinalPrice`: block;
   - more than one payment, regardless of total: block.
6. Only after the guard finds zero exceptions, replace the non-unique `IX_Payments_ReservationId` with the unique index. The guard and index change must be in the same EF migration transaction so failure leaves schema, data, migration history, row counts, and monetary totals unchanged.
7. Keep `Down` non-destructive: it may restore the non-unique index, but it must never synthesize, merge, update, or delete financial rows.
8. Add SQL Server migration regression coverage proving the guard, preservation, unique index, rollback behavior, and clean install/upgrade behavior. EF InMemory is not acceptable evidence.
9. Treat any exception found in a persistent database as a separate controlled data-reconciliation activity:
   - capture and verify a restorable backup first;
   - preserve original rows in an immutable, access-controlled archive or equivalent approved evidence store;
   - obtain explicit per-class business rules and operator approval;
   - reconcile row counts and monetary totals before/after;
   - retain exception and approval evidence;
   - do not include a generic auto-cleanup script in this finding.
10. Document the safe release sequence near the migration instructions:
    - generate and review SQL;
    - run read-only preflight;
    - stop on exceptions;
    - complete separately approved reconciliation and backup/restore rehearsal when required;
    - rerun preflight;
    - apply the reviewed migration;
    - reconcile counts/totals and verify the unique index.

### Alternatives considered

- Keep the current `DELETE` and take a backup first: rejected. A backup does not authorize loss or provide a reconciliation rule, and routine restore would roll back unrelated post-backup activity.
- Automatically sum multiple rows into one payment: rejected. It destroys payment method/date/note provenance and invents rules for duplicates, adjustments, and overpayments.
- Keep the first/latest/largest payment and delete the rest: rejected as arbitrary and financially destructive.
- Archive all exceptions automatically inside the EF migration and then delete/merge live rows: rejected as the default. Archive ownership, retention, immutability, access, recovery, and mapping rules require explicit design and approval; automatic archival still changes live financial meaning.
- Drop the unique index requirement and retain partial payments indefinitely: out of scope. That reverses the approved runtime model rather than remediating migration safety.
- Add only a unique index and let SQL Server fail on duplicates: insufficient. It does not report single non-matching amounts, provide an operator-friendly inventory, or prove preservation/reconciliation.
- Use an EF InMemory test: rejected for migration SQL, transaction, index, and SQL Server semantics.

### Files expected to change

Required in the not-yet-applied migration path:

- `backend/Padelito.Infrastructure/Data/Migrations/20260724201719_EnforceFullReservationPayment.cs`
- `docs/remediation/sql/PRD-BLK-003-preflight.sql`
- `backend/Padelito.Application.Tests/` with a focused SQL Server migration-safety test/harness file
- `README.md` only for the migration preflight/backup/release procedure and correction of directly conflicting payment migration guidance

Change only if generated metadata actually changes:

- `backend/Padelito.Infrastructure/Data/Migrations/20260724201719_EnforceFullReservationPayment.Designer.cs`
- `backend/Padelito.Infrastructure/Data/Migrations/PadelitoDbContextModelSnapshot.cs`

If the migration was already applied to a persistent/shared environment:

- do not amend the existing migration ID;
- replan the exact additive corrective migration, recovery evidence, and reconciliation artifacts before implementation.

### Explicit implementation boundaries

- Do not connect to or modify production, MonsterASP.NET, remote, shared, or persistent databases.
- Do not apply migrations to real data.
- Do not delete, update, merge, re-date, re-method, or fabricate any payment, reservation, or audit record.
- Do not invent exception disposition rules.
- Do not create or execute a reconciliation script until business rules, archive requirements, backup/restore proof, and operator approval are recorded.
- Do not change the full-payment product rule, tenant authorization, reporting semantics, or reservation lifecycle beyond what is strictly required to make the migration safe.
- Do not fix `PRD-BLK-002` trigger/idempotent-script behavior in this finding.
- Do not modify readiness status in plan or implement mode.
- Do not overwrite unrelated dirty-working-tree changes.

### Adjacent/out-of-scope findings

- `PRD-BLK-002` still blocks a complete idempotent-script rehearsal because of unrelated trigger DDL emitted inside EF's idempotent wrapper.
- Product/documentation consistency around legacy partial-payment language is adjacent; only migration/release guidance directly needed by this finding belongs here.
- A durable refunds/adjustments/accounting model is a separate business and architecture decision.
- General backup infrastructure, retention policy, CI/CD, observability, and production deployment are outside this finding.
- Any recovery for data already deleted by an applied copy of this migration requires a separate evidence-driven recovery plan.

## Compatibility and data

- Data-model implications:
  - retain the new unique `Payment.ReservationId` index for new data after preflight succeeds;
  - retain `Amount > 0`, foreign keys, and existing financial records;
  - do not add a misleading database constraint that claims to enforce `Payment.Amount == Reservation.FinalPrice` across tables;
  - the application remains responsible for creating the server-calculated full payment.
- Migration implications:
  - the deployable path must contain no `DELETE`, `UPDATE`, merge, or synthetic `INSERT` against financial rows;
  - guard failure must occur before index changes and before the migration-history row is committed;
  - migration ID strategy depends on whether the ID has been applied persistently and must be resolved before implementation.
- Clean-install implications:
  - an empty database has zero payments, passes the guard, and creates the unique index;
  - global reference catalogs remain unaffected;
  - no application/financial data is inserted by this remediation.
- Upgrade implications:
  - zero-payment reservations remain valid;
  - one exact payment per reservation upgrades without data changes;
  - any non-matching single payment or any multi-payment history blocks the upgrade with all data/schema/history unchanged;
  - the operator must not bypass the guard by deleting or editing records ad hoc.
- Compatibility implications:
  - existing legacy histories may delay release until reviewed;
  - old application versions that permit partial/multiple payments must be stopped or made read-only during the final preflight/migration cutover to prevent new exceptions;
  - current application code and the unique index protect new writes after upgrade, but rolling back the application while retaining the unique index may be incompatible.
- Rollback/roll-forward/recovery strategy:
  - before real upgrade, require a restorable backup and a tested restore checkpoint;
  - on guard failure, roll forward only after approved reconciliation; the failed transactional migration must leave no partial schema/data state;
  - on rehearsal failure, discard only the explicitly disposable test database;
  - `Down` restores index cardinality only and never mutates data;
  - after production cutover, prefer a reviewed roll-forward fix. Application rollback must account for the legacy code's ability to attempt multiple payments.
- Backup or reconciliation requirements:
  - record backup identifier/time, database migration boundary, restore target, restore verification result, operator, and approval;
  - pre/post evidence must include total payment rows, total payment amount, exception count, and per-reservation reconciliation;
  - preserve original exception rows immutably before any approved transformation;
  - document every unresolved record and never delete it merely to satisfy the index.

## Testing and verification

### Regression tests

- Static migration test:
  - the PRD-BLK-003 migration contains no destructive DML against `Payments`, `Reservations`, or `ReservationAudits`;
  - the exception guard precedes index replacement;
  - `Down` contains index operations only.
- SQL Server clean-install test:
  - apply the full chain to a unique disposable empty database;
  - assert zero financial/application rows are created by this migration;
  - assert `IX_Payments_ReservationId` is unique.
- SQL Server compatible-upgrade test:
  - migrate to the predecessor;
  - insert reservations representing zero payments and one exact full payment;
  - record payment IDs, counts, totals, dates, methods, notes, and migration-history state;
  - migrate to latest;
  - assert all values are byte-for-byte/logically unchanged and the unique index exists.
- SQL Server incompatible-upgrade theory/cases:
  - one partial payment;
  - one overpayment;
  - two payments totaling exactly `FinalPrice`;
  - two payments below `FinalPrice`;
  - two payments above `FinalPrice`;
  - for each, assert migration failure, unchanged row count and monetary total, unchanged payment fields, old non-unique index retained, new migration-history row absent, and no audit/reservation mutation.
- Unique-index behavior:
  - after a compatible upgrade, a second payment for the same reservation fails at SQL Server level and leaves the first payment unchanged.
- Rollback rehearsal:
  - from a compatible upgraded disposable database, migrate down to the predecessor;
  - assert all payment records/fields/totals remain unchanged and the index is non-unique again.
- Preflight test:
  - run against fixtures for every class;
  - verify correct classification and totals;
  - verify it performs no DML and repeated execution leaves the database unchanged.
- Existing service/API regression tests remain required, but they are supplementary rather than migration proof.

### Negative/bypass cases

- Reservations with zero payments are not falsely blocked.
- Multiple payments summing exactly to `FinalPrice` are still blocked because they violate cardinality.
- A single payment whose amount differs by the smallest supported decimal unit is blocked.
- The migration cannot partially drop/create indexes before raising the exception.
- The generated SQL contains no hidden payment deletion/update in this or adjacent migration bodies.
- Re-running the idempotent script after successful application performs no payment mutation.
- Concurrent legacy writes are prevented during cutover; preflight evidence is not accepted if the old writer remains active between preflight and migration.
- Operator failure/error messages expose counts and remediation guidance, not customer personal data or secrets.

### Verification commands

Run from the repository root after implementation, using only restored dependencies and disposable local/test infrastructure:

```powershell
git status --short
git diff --check
git diff -- backend/Padelito.Infrastructure/Data/Migrations backend/Padelito.Infrastructure/Data/PadelitoDbContext.cs backend/Padelito.Application.Tests README.md docs/remediation
dotnet build backend/PadelitoV2.slnx -c Release --no-restore
dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj -c Release --no-build --no-restore --filter "FullyQualifiedName~PaymentMigrationSafety"
dotnet test backend/Padelito.Application.Tests/Padelito.Application.Tests.csproj -c Release --no-build --no-restore
dotnet tool run dotnet-ef migrations list --no-build --configuration Release --project backend/Padelito.Infrastructure --startup-project backend/Padelito.Api
dotnet tool run dotnet-ef migrations script --idempotent --no-build --configuration Release --project backend/Padelito.Infrastructure --startup-project backend/Padelito.Api --output <DISPOSABLE_TEMP_PATH>\PRD-BLK-003-idempotent.sql
rg -n -i "DELETE\s+(FROM\s+)?(\[dbo\]\.)?\[?Payments\]?|UPDATE\s+(\[dbo\]\.)?\[?Payments\]?|MERGE\s+(\[dbo\]\.)?\[?Payments\]?" <DISPOSABLE_TEMP_PATH>\PRD-BLK-003-idempotent.sql
```

The SQL Server integration harness and preflight command must:

- accept only a local/test-only SQL Server endpoint;
- generate a unique disposable database name;
- verify the database is new/empty before use;
- refuse production, remote, shared, or ambiguous connection settings;
- drop only its exact validated disposable database in a `finally` path;
- record commands, SQL Server version, migration boundaries, row counts, totals, index metadata, and exact results.

The production-like snapshot rehearsal is operator-run only after the snapshot is anonymized, isolated, approved, and proven disposable. This plan does not authorize access to the source or any live database.

### Required environment

- Compatible .NET 10 SDK and restored repository-local `dotnet-ef` `10.0.9`.
- A local disposable SQL Server with behavior matching the target SQL Server version; EF InMemory is insufficient.
- No production credentials.
- Synthetic fixtures for all compatibility/exception classes.
- For final operational evidence, an approved anonymized production-like snapshot and a separate tested backup/restore environment.

### Evidence required for resolution

- Persistent-environment migration inventory proves which migration path was valid.
- Reviewed generated SQL contains no destructive or synthetic financial DML.
- Clean and compatible upgrades preserve exact payment row counts, totals, and fields.
- Every incompatible fixture blocks transactionally with unchanged data, index, and migration history.
- The preflight classifies all fixtures correctly and is read-only/idempotent.
- Unique-index and rollback rehearsals pass on SQL Server.
- A production-like snapshot has zero unexplained exceptions, or every exception has approved reconciliation plus immutable-original, pre/post reconciliation, and backup/restore evidence.
- Old partial-payment writers are excluded during cutover.
- Focused and full backend validations pass.
- Relevant diff matches the approved scope and preserves unrelated work.
- The separate `PRD-BLK-002` idempotent-script blocker is either resolved or explicitly prevents claiming full-chain idempotent verification; it must not be hidden or attributed to this finding.

## Delivery controls

- Risks:
  - editing an already-applied migration would fork migration semantics across environments;
  - an incomplete inventory could miss real legacy exceptions;
  - a generic consolidation rule could erase payment method/date/note provenance;
  - preflight can become stale if legacy writers remain active;
  - the unique index enforces cardinality but not the cross-table amount invariant;
  - current dirty/untracked changes can drift before implementation;
  - `PRD-BLK-002` currently prevents the complete idempotent script from executing.
- Dependencies:
  - authoritative migration-history inventory for every persistent environment;
  - business owner for payment-history semantics;
  - database operator for backup/restore and isolated rehearsal;
  - restored .NET/EF toolchain and local SQL Server;
  - a release window that stops legacy payment writes;
  - resolution or separately recorded limitation of the `PRD-BLK-002` idempotent trigger issue.
- Assumptions:
  - reservations with no payment remain valid/unpaid;
  - for new writes, the intended product rule is one server-calculated full payment per reservation;
  - `Payment`, `Reservation`, and `ReservationAudit` are financially sensitive club-owned records;
  - no reconciliation semantics can be inferred from amount equality alone.
- Human decisions resolved on `2026-07-27`:
  1. The user confirmed that `20260724201719_EnforceFullReservationPayment` returned no migration-history result in every persistent/shared database. The pending migration may therefore be amended in place.
  2. This is a demo MVP. The project owner approved the policy; the test administrator and receptionist are the operational roles allowed to collect the one full payment supported by the application.
  3. A restorable backup is the approved original-data preservation mechanism before any real migration or later transformation. No transformation is authorized by this implementation.
  4. No automatic reconciliation is approved. Any partial, multiple, duplicate, adjusted, refunded, overpaid, or otherwise non-matching history must stop the migration without writes and be handled under a separate approved plan.
  5. The user approved the database-operator procedure: capture a current backup, prove it can be restored in an isolated database, and reconcile payment row counts and totals before applying a real upgrade.
  6. The user approved a maintenance window that stops all old application writers, runs preflight, aborts on exceptions, applies the reviewed migration, deploys only the full-payment version, verifies rows/totals/indexes, and then reopens service.
- Definition of done:
  1. The migration-history decision is recorded and the compatible migration strategy is used.
  2. No deployable migration or remediation artifact deletes, overwrites, merges, or fabricates financial/history rows.
  3. A read-only exception preflight exists, is documented, and is tested against all compatibility classes.
  4. Migration guard failure is transactional and preserves data, schema, totals, and migration history.
  5. Compatible data upgrades unchanged and the unique reservation index is enforced.
  6. Clean install, compatible/incompatible upgrade, unique-index, rollback, and preflight tests pass on disposable SQL Server.
  7. Generated SQL review finds no destructive financial DML.
  8. Any persistent exceptions have explicit business approval, immutable-original evidence, exact pre/post row and monetary reconciliation, and tested backup/restore proof.
  9. Focused and full regression suites pass, and relevant dirty-worktree drift has been revalidated.
  10. Verification evidence is appended to this plan and only `PRD-BLK-003` is updated in the readiness report during `verify`.

## Implementation record

Implemented on `2026-07-27` against branch `main`, commit
`7986f1df158c9c252f093e7a434d3aa113df7981`, with the pre-existing dirty
working tree preserved.

### Decisions applied

- The user confirmed that the migration ID was absent from every
  persistent/shared migration history, so the pending migration was amended in
  place.
- The MVP accepts exactly one server-calculated full payment per reservation.
- No automatic reconciliation or financial-row transformation is authorized.
- A real upgrade requires a tested backup and an exclusive maintenance window.

### Changes

- Replaced the destructive payment deletion in
  `20260724201719_EnforceFullReservationPayment.cs` with a transactional,
  fail-closed compatibility guard.
- The guard allows zero payments or exactly one payment equal to
  `Reservations.FinalPrice`. It throws before index replacement for any
  non-matching single payment or any multiple-payment history.
- Added `docs/remediation/sql/PRD-BLK-003-preflight.sql`, which reports:
  migration-history state, global payment rows/totals, compatibility-class
  counts, exception totals/variance, and the constituent payment records.
- Added `PaymentMigrationSafetyTests.cs` to prove operation order, guard
  predicates, absence of financial DML, transactional execution, unique-index
  creation, and non-destructive rollback operations.
- Updated README payment semantics and documented explicit-target preflight,
  tested backup, maintenance window, and fail-closed release behavior.
- Recorded the user's six approved decisions in this plan.

### Scope and safety

- No payment, reservation, audit, production data, secret, or infrastructure
  was read or changed.
- No database was contacted and no migration or preflight was applied.
- No reconciliation or archive implementation was created.
- Designer/model snapshot metadata did not require changes because index model
  metadata was already correct.
- The readiness finding status was not changed in implement mode.
- No material deviation from the approved implementation scope occurred. The
  planned real SQL Server data-preservation rehearsals remain verification work,
  not evidence claimed by this implementation record.

### Validation results

- Initial Release build: `FAIL`, 2 compile errors (`CS0122`) because the new
  test used `nameof` on protected EF migration methods. Corrected locally to
  reflection names `"Up"` and `"Down"`.
- `dotnet build backend/PadelitoV2.slnx -c Release --no-restore`: `PASS`;
  0 warnings, 0 errors. The installed preview SDK emitted informational
  `NETSDK1057` messages.
- Focused test command with filter
  `FullyQualifiedName~PaymentMigrationSafety`: `PASS`; 2 passed, 0 failed,
  0 skipped.
- Full backend test command: `PASS`; 65 passed, 0 failed, 0 skipped.
- Source/preflight destructive-payment-DML scan: `PASS`; 0 matches.
- `git diff --check`: `PASS`; exit 0. Git emitted existing LF-to-CRLF
  conversion warnings only.
- First tool restore inside the restricted sandbox: `FAIL`, because the user
  NuGet configuration was not readable. Approved repository tool restore
  outside the sandbox: `PASS`; restored `dotnet-ef` 10.0.9.
- First idempotent-script inspection used the stale default Debug output:
  generated 30,465 bytes with 0 destructive payment-DML matches but did not
  contain the new guard. This was not accepted as evidence.
- Idempotent script regenerated explicitly from Release:
  `PASS`; 30,588 bytes, exactly 1 `THROW 51003` guard, and 0 destructive
  `DELETE`/`UPDATE`/`MERGE`/`INSERT` matches against `Payments`. The temporary
  script was deleted after inspection and was never executed.

## Verification evidence

- Verification date: `2026-07-27`
- Branch: `main`
- Commit or working-tree state: commit
  `7986f1df158c9c252f093e7a434d3aa113df7981`; dirty working tree containing
  the approved PRD-BLK-003 implementation plus pre-existing related and
  unrelated user work. Evidence is reproducible from this exact working tree,
  not from `HEAD` alone.
- Finding: `PRD-BLK-003`
- Approved scope: replace destructive cleanup with fail-closed preflight/guard,
  preserve every financial row, enforce the unique index only for compatible
  data, document backup/cutover, and add regression coverage.
- Diff reviewed: migration, designer/snapshot context, DbContext, payment API,
  DTO/service/repository/runtime callers, payment tests, README, preflight,
  remediation plan, readiness report, and all relevant tracked/untracked Git
  state.
- Scope deviations: no application-source scope deviation. Verification used
  an explicit SQL Server 2019 LocalDB harness assembled from approved commands
  rather than committing a permanent database harness. The full idempotent
  chain remains non-executable because of the separately documented
  `PRD-BLK-002` trigger-wrapper issue; the isolated idempotent PRD-BLK-003
  segment passed twice.

### Root-cause verification

- Property verified: upgrading to the one-full-payment schema never deletes,
  updates, merges, replaces, or fabricates financial/history records. Compatible
  histories upgrade unchanged; incompatible histories abort transactionally
  before index or migration-history changes.
- Method:
  - inspected migration operations and generated SQL;
  - applied clean and predecessor-to-latest migrations on SQL Server 2019
    LocalDB;
  - compared reservation/payment/audit counts, monetary totals, payment
    checksums, index metadata, and migration history before/after;
  - exercised zero payment, one exact payment, one partial by `0.01`, one
    overpayment by `0.01`, two payments totaling exactly the final price, two
    below, and two above;
  - ran preflight twice against compatible and incompatible fixtures;
  - tested duplicate insertion after migration and `Down` rollback;
  - performed `COPY_ONLY` backup with checksum, `RESTORE VERIFYONLY`, isolated
    restore, preflight, and migration of the restored representative fixture;
  - executed the isolated idempotent PRD-BLK-003 script twice.
- Result:
  - clean install: reservations/payments `0/0`, amount `0`, unique index `1`,
    migration-history row `1`;
  - compatible upgrade: snapshot remained
    `Reservations=2, FinalPrice=200.00, Payments=1, Amount=100.00,
    PaymentChecksum=1720245663, Audits=1`;
  - duplicate payment failed with SQL unique-key error and left the snapshot
    unchanged;
  - rollback restored non-unique index/history `0/0` and preserved the snapshot;
  - incompatible aggregate fixture intentionally had total reservation price
    and payment amount both `500.00`, proving aggregate equality is not a
    bypass. Preflight reported 2 non-matching single-payment reservations and
    3 multi-payment reservations, including the exact-total pair;
  - incompatible migration returned guard `51003`; snapshot remained
    `Reservations=5, FinalPrice=500.00, Payments=8, Amount=500.00,
    PaymentChecksum=1353469928, Audits=1`; index remained non-unique and
    migration-history row remained absent;
  - backup/restore snapshot remained
    `Reservations=2, FinalPrice=200.00, Payments=1, Amount=100.00,
    PaymentChecksum=2012023008, Audits=1`; restored migration also preserved it;
  - isolated idempotent segment snapshot remained
    `Reservations=1, FinalPrice=100.00, Payments=1, Amount=100.00,
    PaymentChecksum=-1256612733` before, after run 1, and after run 2.
- Bypasses/incomplete paths checked:
  - zero payments are allowed;
  - single exact payment is allowed;
  - `0.01` under/over amounts are blocked;
  - all multi-payment cardinalities are blocked even when total equals
    `FinalPrice`;
  - guard failure precedes index/history mutation and leaves audit data intact;
  - unique index rejects a second direct-SQL payment;
  - `Down` changes only index cardinality;
  - preflight repeated output is identical and performs no persistent writes;
  - admin/reception API policy, server-calculated amount, rejection of
    client-supplied amount/date, and second-payment service behavior remain
    covered.
- Remaining limitations:
  - this MVP has no real production dataset; the user confirmed the pending
    migration ID was absent from every persistent/shared database. Verification
    therefore used synthetic representative SQL Server fixtures rather than
    remote or real data;
  - any future real upgrade still requires the approved exclusive maintenance
    window and a fresh tested backup;
  - the unique index proves at-most-one payment; amount equality remains
    enforced by the application and by this upgrade guard for existing data;
  - the full idempotent chain cannot currently be executed because
    `PRD-BLK-002` emits `CREATE OR ALTER TRIGGER` inside EF's idempotent
    wrapper. Static review of the full script passed, and the PRD-BLK-003
    segment itself executed twice successfully.

### Tests and commands

| Command | Purpose | Result | Exact evidence |
|---|---|---|---|
| `git status --short` plus scoped/full diffs | Revalidate snapshot, overlap, and scope | `PASS` | branch `main`, HEAD `7986f1df...`; relevant dirty/untracked files matched plan; no material drift |
| initial sandboxed `sqlcmd` LocalDB identification | Validate local test target | `FAIL` | ODBC 17 encryption/security-package error before connection |
| approved `sqlcmd -S '(localdb)\MSSQLLocalDB' ... SERVERPROPERTY(...)` | Validate target outside restricted sandbox | `PASS` | SQL Server `15.0.4382.1`, Express Edition, server `Ferdi\LOCALDB#D5826C61` |
| clean `dotnet-ef database update` on unique LocalDB | Clean migration chain | `PASS` | counts `0/0/0.00`, unique index `1`, history `1`, database removed (`EXISTS_AFTER=0`) |
| first compatible-fixture seed without `sqlcmd -I` | Prepare upgrade fixture | `FAIL` | SQL error `1934`, `QUOTED_IDENTIFIER` incorrect; database removed |
| diagnostic seed with visible output | Identify fixture setup failure | `PASS` | confirmed SQL error `1934`; diagnostic database removed |
| compatible preflight twice + latest migration + duplicate insert + rollback | Prove preservation, idempotent preflight, unique index, and non-destructive `Down` | `PASS` | repeated preflight identical; before/after/post-duplicate/rollback snapshots identical; index/history `1/1` after Up and `0/0` after Down; database removed |
| five-case incompatible preflight and migration attempt | Prove fail-closed behavior and bypass resistance | `PASS` | preflight reported `MULTIPLE_PAYMENTS=3/6/300.00`, `ONE_NON_MATCHING_PAYMENT=2/2/200.00`; migration exit `1`, guard detected; snapshot/index/history unchanged; database removed |
| `BACKUP ... COPY_ONLY, CHECKSUM`; `RESTORE VERIFYONLY`; isolated `RESTORE`; preflight; migrate | Prove backup/restore checkpoint and restored-data preservation | `PASS` | 5,689,344-byte backup; verify-only passed; source/restored/post-migration snapshots identical; both databases and `.bak/.mdf/.ldf` removed |
| `dotnet build backend/PadelitoV2.slnx -c Release --no-restore` | Compile current implementation | `PASS` | 0 warnings, 0 errors; preview-SDK `NETSDK1057` informational messages only |
| focused `dotnet test ... --filter "FullyQualifiedName~PaymentMigrationSafety"` | Migration-operation regression tests | `PASS` | 2 passed, 0 failed, 0 skipped |
| full `dotnet test ... --no-build --no-restore` | Backend regression suite | `PASS` | 65 passed, 0 failed, 0 skipped |
| source/preflight destructive-DML scan | Detect payment mutation bypass | `PASS` | 0 destructive payment-DML matches |
| `dotnet-ef migrations list --no-connect` | Confirm migration ordering | `PASS` | 10 migrations; PRD-BLK-003 is latest |
| full `dotnet-ef migrations script --idempotent --configuration Release` plus scan | Inspect deployable SQL | `PASS` | 30,588 bytes; one `THROW 51003`; 0 destructive payment-DML matches; temporary script removed |
| predecessor-to-PRD-BLK-003 idempotent segment executed twice | Prove PRD-BLK-003 repeated-script behavior | `PASS` | 1,545-byte segment; identical financial checksum before/after both runs; index/history `1/1`; database/script removed |
| `git diff --check` | Patch integrity | `PASS` | exit 0; existing LF-to-CRLF warnings only |

Every disposable database name was validated against the
`Padelito_PRD_BLK_003_*_<32 hex>` pattern before use. All databases reported
`EXISTS_AFTER=0`; all temporary backup, data, log, and SQL files reported absent
after cleanup. No remote database, production data, credential, deployment, or
infrastructure was accessed or changed.

### Change evidence

- Summary: destructive migration cleanup was replaced with a transactional
  guard and read-only preflight; compatible records are preserved and
  incompatible records block the migration.
- Files changed:
  - `backend/Padelito.Infrastructure/Data/Migrations/20260724201719_EnforceFullReservationPayment.cs`
  - `backend/Padelito.Application.Tests/PaymentMigrationSafetyTests.cs`
  - `docs/remediation/sql/PRD-BLK-003-preflight.sql`
  - `README.md`
  - `docs/remediation/PRD-BLK-003-plan.md`
  - `docs/production-readiness.md` during verification only
- Tests added/updated: 2 focused migration-operation tests; existing payment,
  API, bootstrap, reporting, dashboard, reservation, authentication, and
  lifecycle coverage remained green.
- Unrelated behavior checked: payment endpoint remains
  `AdminOrReception`; tenant-scoped repository lookup remains intact; server
  continues to calculate `FinalPrice`; second payments and client-supplied
  amount/date remain rejected; reports/tests compile and pass.

### Status decision

- Previous status: `OPEN`
- New status: `RESOLVED`
- Evidence supporting status: root-cause DML is absent; generated SQL is
  non-destructive; clean, compatible, incompatible, rollback, duplicate,
  preflight, backup/restore, and repeated idempotent-segment behavior passed on
  real SQL Server; regression coverage and exact evidence are recorded.
- Unresolved portions: none within `PRD-BLK-003`. The full-chain idempotent
  trigger-wrapper failure remains owned by `PRD-BLK-002`, and normal real
  release controls remain mandatory.
- Recommended next command: `/production-remediation plan PRD-BLK-002` to
  remediate its remaining idempotent trigger-DDL failure, then verify it again.
