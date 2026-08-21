# Database Change Safety Report: Approved production identity topology

## 1. Decision

`SAFE WITH CONDITIONS`. The approved remediation is bounded, transactional,
idempotent, and rehearsed against the exact topology observed in production.
Deployment still requires a newly created backup whose restoration is proven in
an isolated MonsterASP.NET database, maintenance mode, and coordinated
schema/application rollout.

## 2. Scope

- Baseline release commit: `a9fe327` plus the approved follow-up diff.
- Provider/ORM: SQL Server 2025 and EF Core 10.0.9.
- Data source evidence captured before this audit: one active club, Person 2
  `Fernando Perez` as active client, Person 3 `Juan Perez` as inactive employee
  without a user, active `admin`, inactive `jorge`, no `juanperez`, three exact
  payments totaling 109.88, and no `Users.IsDemo` column.
- Reviewed code: `DemoAccessProvisioner`, its tests and configuration.
- Reviewed SQL: privacy preflight, update, and final postflight.
- Reviewed constraints: unique filtered `People.Dni`, unique `Users.Username`,
  unique `Users.EmployeeId`, foreign keys, and migration ordering.
- Production was not contacted or mutated during this audit phase.

## 3. Change Summary

| Object | Change | Source | Compatibility | Risk |
| ------ | ------ | ------ | ------------- | ---- |
| `People.Id=2` | Rename Fernando to Ferdinando; replace ID, phone, email | `demo-privacy-update.sql` | Application compatible | Backup-only data rollback |
| `People.Id=3` | Replace ID, phone, email; activate | `demo-privacy-update.sql` | Application compatible | Authorization preparation |
| `Users` | Create active demo `juanperez` for Juan's employee | `DemoAccessProvisioner` | Requires new schema/app | Secret and ordering sensitive |
| `Users` | Preserve inactive `jorge` | Pre/postflight guards | Backward compatible | Low |
| `Users.IsDemo` | Add required bit default false | `AddDemoUserFlag` | Coordinated deployment | Existing DB1 remains |

## 4. Migration Inventory

The schema migration remains unchanged from the prior report: one additive
`bit NOT NULL DEFAULT 0` column, with no data deletion, table rebuild, or index
change. Existing users remain real until the explicit provisioner succeeds.

The data updater performs two bounded `People` updates. User creation is not in
versioned SQL because it needs a private password hash; it occurs in the startup
provisioner's serializable transaction after the schema exists.

## 5. Critical Findings

### DB-101 - Identity remediation is irreversible in place

**Priority:** `DB1`

The updater overwrites personal fields and the original private identifier is
intentionally absent from source and logs. Recovery requires the pre-change
backup. The script locks and validates exact rows, relationships, fictitious-ID
collisions, the inactive `jorge` invariant, and row counts before commit.

**Deployment condition:** prove the new backup restores into a separate
database before executing the updater.

### DB-102 - Rollback to the old application re-enables public real writes

**Priority:** `DB1`

The old release ignores `IsDemo`; it must never serve active public credentials
after provisioning. Failure recovery must retain maintenance mode and either
restore database plus artifact together or deactivate `admin` and `juanperez`.

### DB-103 - Reception creation requires a temporary private hash

**Priority:** `DB2`

The provisioner accepts the hash only when `juanperez` is absent, validates the
Identity hash envelope, requires exact active Juan Perez PersonId 3 in the only
club with no linked user, and relies on database unique indexes for username and
employee ownership. A wrong but structurally valid hash would create an account
that cannot authenticate.

**Deployment condition:** compare the copied value to the local English
`juanperez` hash without printing either value, then remove the environment
secret immediately after successful provisioning.

## 6. Data Preservation Analysis

No rows are deleted. Names, keys, employee/client relationships, reservations,
payments, and audits are preserved except for the explicitly approved first-name
normalization and activation. The updater uses `XACT_ABORT`, `SERIALIZABLE`,
`UPDLOCK`, `HOLDLOCK`, exact row counts, collision checks, and postconditions.

## 7. Constraints and Relationships

- Person 2 must be an active client and not an employee.
- Person 3 must be Juan Perez, an employee of the sole active club, with either
  no user or the exact active Reception `juanperez` target.
- A `juanperez` attached to any other employee blocks both scripts and startup.
- Unique indexes prevent duplicate fictitious IDs, usernames, and user/employee
  links even under concurrent access.
- Any active `jorge` blocks preflight; final postflight requires it inactive.

## 8. Index and Query Impact

No index changes are introduced by this follow-up. The bounded point updates and
single-user insert use existing primary/unique indexes. Maintenance mode removes
interactive contention.

## 9. Concurrency and Transaction Safety

Privacy changes and account provisioning are separate serializable
transactions. The updater commits before migration/startup, so an application
startup failure leaves the anonymous profiles applied but no partial user
provisioning; the tested backup remains the recovery boundary. Provisioning
creates `juanperez`, creates or verifies `Ferdi`, and marks both public users in
one transaction.

## 10. Backfill Plan Requirements

There is no bulk backfill. Two exact people are corrected, one user may be
created, and two public flags are set. Batching is unnecessary for this bounded
demo database.

## 11. Compatibility Matrix

| Application | Schema/data | Compatible? | Notes |
| ----------- | ----------- | ----------- | ----- |
| Previous | Previous | Yes, publicly unsafe | Existing public Admin writes SQL |
| Previous | New | Technically runs, operationally prohibited | Ignores demo flag |
| New | Previous | No | Requires `Users.IsDemo` |
| New | New before provisioning | Starts only with exact secrets/topology | Fail closed |
| New | New after provisioning | Yes | Intended state |

## 12. Recommended Deployment Sequence

1. Create a fresh full backup and restore it into a new isolated database.
2. Validate restored migration history, identities, counts, relationships, and
   payment total against the production baseline.
3. Put the website in maintenance and confirm workers/requests stop.
4. Run privacy and payment preflights; require exact pass.
5. Run the privacy updater, then the reviewed idempotent EF migration script.
6. Configure the rotated JWT key, private Ferdi password, and temporary
   Reception password hash.
7. Deploy and start once; require atomic provisioning success.
8. Run final privacy/account/schema/financial postflight and application smoke
   tests before reopening traffic.
9. Disable provisioning, remove both temporary provisioning secrets, restart,
   and repeat authentication smoke tests.

## 13. Rollback and Recovery

Before traffic reopens, restore the tested backup and prior artifact together
for any failed check. Code-only rollback is prohibited. If immediate full
restoration is unavailable, keep maintenance active and disable both public
accounts. No inverse SQL will reconstruct the removed private data.

## 14. Validation Checklist

- 74 automated tests, Release build, lint, frontend build, and publish pass.
- Exact production-shape preview passed `READY`, `UPDATED`, seven migrations,
  first startup, and final `PASS`.
- Preview proved the new `juanperez` hash equals the local English source hash
  without displaying it.
- Preview final state: `admin=active/demo`, `juanperez=active/demo`,
  `Ferdi=active/real`, `jorge=inactive/real`.
- Production backup isolated restore and the same checks remain mandatory.

## 15. Findings Summary

| ID | Priority | Area | Risk | Required action |
| -- | -------- | ---- | ---- | --------------- |
| DB-101 | DB1 | Recovery | Anonymous overwrite needs backup | Prove isolated restore |
| DB-102 | DB1 | Compatibility | Old app permits public SQL writes | Coordinated rollback only |
| DB-103 | DB2 | Credential transfer | Structurally valid wrong hash prevents login | Equality check and smoke login |

No DB0 or DB3 findings were identified.

## 16. Conditions for Approval

- Fresh backup restoration is proven in an isolated MonsterASP.NET database.
- Restored and production preflights match the captured baseline exactly.
- Maintenance remains active through final postflight and smoke tests.
- The temporary Reception hash equals the local English account hash and both
  provisioning secrets are deleted after use.

## 17. Deferred Concerns

The unrelated modified English seed and remediation scripts remain outside this
release. The preview-SDK informational warning remains unchanged.

## 18. Final Recommendation

Proceed only after the isolated restore checkpoint. The newly approved identity
shape is fully accounted for and rehearsed; any mismatch at backup restore,
preflight, provisioning, postflight, or smoke test requires stopping under
maintenance without partial recovery shortcuts.
