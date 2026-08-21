# Database Change Safety Report: Demo accounts production rollout

## 1. Decision

`SAFE WITH CONDITIONS`. No destructive schema operation or data-deletion path
was found. Approval remains conditional on an isolated restore test of the
production backup, exact production preflight results, a maintenance window,
and keeping the previous application offline because it would let the public
credentials write to SQL Server.

## 2. Scope

- Baseline: branch `production-runbooks-and-demo-data` at
  `f46faf661da36c3396ade7a4518f60effd8fc52d` plus the reviewed working-tree
  release candidate.
- Provider/ORM: SQL Server through EF Core 10.0.9.
- Schema: `20260801181253_AddDemoUserFlag`, entity, mapping snapshot, and
  generated idempotent SQL.
- Data: the three `docs/operations/sql/demo-privacy-*.sql` scripts and startup
  `DemoAccessProvisioner`.
- Deployment: `docs/operations/demo-access-rollout.md` and the supplied
  MonsterASP.NET maintenance/backup procedure.
- Production schema/history and volume were not contacted during this audit;
  they must be captured by the production preflight.

## 3. Change Summary

| Object | Change | Source | Compatibility | Risk |
| ------ | ------ | ------ | ------------- | ---- |
| `Users.IsDemo` | Add required `bit`, default `0` | `20260801181253_AddDemoUserFlag.cs` | Additive for old app; required by new app | Low schema risk |
| `People` rows 2 and 3 | Replace customer ID, phone, email | `demo-privacy-update.sql` | Application compatible | Irreversible without backup |
| `Users` public/private rows | Mark two demo users and create/validate `Ferdi` | `DemoAccessProvisioner.cs` | Requires new application | Coordinated rollout |

## 4. Migration Inventory

`AddDemoUserFlag` adds one non-nullable SQL Server `bit` with a semantic default
of false. Existing users therefore remain real until the explicit provisioner
runs. The migration has no raw SQL, index rebuild, data rewrite, delete, or
table/column drop in `Up`. Its `Down` drops the column and is not an acceptable
production rollback while public credentials remain active.

The generated 954-line idempotent script contained the expected guarded
`ALTER TABLE [Users] ADD [IsDemo] bit NOT NULL DEFAULT 0` and no `DROP TABLE`,
`DROP COLUMN`, or `DELETE FROM`. The temporary generated file was removed after
inspection.

## 5. Critical Findings

### DB-001 - Old application rollback re-enables real public writes

**Priority:** `DB1`

**Affected objects:** `Users.IsDemo`, request database selection, `admin`,
`juanperez`.

**Evidence:** the new middleware routes signed demo users to isolated InMemory
contexts; the previous release has no such persistence boundary.

**Risk:** deploying the previous application while public accounts stay active
would direct their writes to SQL Server even though the rows remain marked
demo.

**Required condition:** maintenance must remain active during failure recovery.
Rollback must atomically restore the backup and artifact or deactivate both
public users before serving traffic.

### DB-002 - Privacy mutation depends on a proven restore checkpoint

**Priority:** `DB1`

**Affected objects:** `People.Id` 2 and 3.

**Evidence:** the script intentionally overwrites three profile fields and does
not retain the previous identifier. It is guarded by exact person, relationship,
role, active-club, and uniqueness fingerprints and executes under a serializable
transaction with `XACT_ABORT`.

**Risk:** after commit, the original profile values can be recovered only from
the pre-change backup.

**Required condition:** prove restoration into an isolated database before the
mutation. A backup that merely exists is insufficient.

### DB-003 - New application and old schema are incompatible

**Priority:** `DB2`

**Affected objects:** all authenticated user queries reading `IsDemo`.

**Risk:** starting the new artifact before the migration produces SQL errors and
prevents safe authentication/provisioning.

**Required condition:** apply the reviewed idempotent migration before the first
new-application startup and verify migration history afterward.

## 6. Data Preservation Analysis

The schema addition preserves every existing row. The privacy update changes
only the approved fields on exactly two fingerprinted rows, preserves names,
keys, client/employee/user relationships, reservations, payments, and audits,
and rolls back on an unexpected row count or failed postcondition. Candidate
customer-ID collisions are rejected before mutation. No source-reviewed path
deletes data.

## 7. Constraints and Relationships

`People.Dni` has a unique filtered index. The scripts check both fictitious
values against all other people before updating. Person primary keys and all
foreign keys remain unchanged. Provisioning validates one active club, distinct
public accounts, exact roles, active state, username/profile uniqueness, and
commits atomically.

## 8. Index and Query Impact

No index is added or removed. The new `bit` is used after authentication and
does not justify an index for this single-club demo workload. Production row
volume is unknown, but adding the defaulted column may briefly take a schema
modification lock; maintenance mode removes interactive contention.

## 9. Concurrency and Transaction Safety

The privacy updater uses `SERIALIZABLE`, `UPDLOCK`, `HOLDLOCK`, `XACT_ABORT`,
row-count checks, and postconditions. Provisioning uses a serializable EF
transaction. Demo writes are serialized per session in the application. The
maintenance window must also stop the lifecycle worker so it cannot race the
snapshot used for production verification.

## 10. Backfill Plan Requirements

No bulk backfill is required. The false default is intentional for every
existing user; the provisioner changes only the two explicitly configured
public users. The two privacy rows are a bounded, idempotent correction rather
than a batched backfill.

## 11. Compatibility Matrix

| Application | Schema | Compatible? | Notes |
| ----------- | ------ | ----------- | ----- |
| Previous | Previous | Yes, but unsafe publicly | Public credentials write real data |
| Previous | New | Technically yes, operationally unsafe | Ignores `IsDemo` |
| New | Previous | No | Queries require `Users.IsDemo` |
| New | New | Yes | Requires successful provisioning and rotated JWT key |

## 12. Recommended Deployment Sequence

1. Record release, target, migration history, row counts, and financial totals.
2. Create a backup and prove an isolated restore.
3. Enable `app_offline.htm` and confirm requests/workers stop.
4. Run privacy preflight and the existing payment preflight; require exact pass.
5. Run privacy update, then the reviewed idempotent EF migration.
6. Run privacy/schema/relationship/financial postflights.
7. Configure private environment settings and a new JWT key.
8. Deploy and start once to provision the exact accounts.
9. Verify SQL flags and end-to-end behavior before removing maintenance.
10. Disable provisioning, delete its password secret, restart, and repeat smoke
    checks.

## 13. Rollback and Recovery

Code-only rollback is prohibited while public users are active. Before traffic
is reopened, failure recovery must either restore the tested database backup and
previous artifact together or leave both demo users disabled. Privacy values are
not inverted by SQL; backup restoration is the only approved data rollback.

## 14. Validation Checklist

- Build, 73 automated tests, lint, frontend build, and Release publish pass.
- Disposable English database rehearsal passes privacy pre/update/postflight,
  migrations, provisioning, demo reset/isolation, and private persistence.
- Production backup restore is demonstrated in an isolated database.
- Production identity, uniqueness, payment, migration-history, and totals
  preflights match exactly.
- Postflight proves two anonymous profiles, three expected `IsDemo` flags,
  preserved relationships/totals, and no startup or clone errors.

## 15. Findings Summary

| ID | Priority | Area | Risk | Required action |
| -- | -------- | ---- | ---- | --------------- |
| DB-001 | DB1 | Compatibility | Old app permits real demo writes | Controlled rollback or deactivate users |
| DB-002 | DB1 | Recovery | Privacy overwrite is irreversible in place | Prove isolated backup restore |
| DB-003 | DB2 | Ordering | New app requires new column | Migrate before startup |

No DB0 or DB3 findings were identified.

## 16. Conditions for Approval

- Exact production preflights pass without identity or uniqueness drift.
- Backup restoration is proven in an isolated database.
- Maintenance remains active through postflight and smoke tests.
- Deployment order and failure recovery follow this report.

## 17. Deferred Concerns

Production volume, SQL Server edition, and hosting-plan restore capacity are
unknown until panel inspection. The repository also contains unrelated English
seed/remediation working-tree changes; they are outside this release scope.

## 18. Final Recommendation

Proceed only under the documented conditions. The database changes themselves
are narrow and rehearsed, but missing restore evidence or a mismatch in the
production fingerprints changes the decision to stop without mutation.
