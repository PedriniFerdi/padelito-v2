# English demo production rollout

## Scope

This is an explicit data correction for the known MonsterASP.NET
demo/portfolio database. It does not apply EF migrations.

The data correction changes authorization catalog values from
`Administrador`/`Recepcion` to `Admin`/`Reception`. The backend running during
the correction must therefore use the canonical English authorization roles.
Commit `19bfbb3` is not compatible: its policies still require the Spanish role
names. A compatible backend includes commit `7e4c58b` (or an independently
reviewed equivalent) and uses `Admin`/`Reception` policies while canonicalizing
legacy Spanish role names during login.

Do not infer backend compatibility from the frontend bundle. Confirm the
deployed backend release/commit independently. If the backend is older than the
compatibility change, deploy and smoke-test a reviewed compatible release as a
separate operation before running any SQL.

The approved scripts are:

1. `docs/remediation/sql/english-demo-preflight.sql`
2. `backend/Padelito.Infrastructure/Data/Seed/english-demo-seed.sql`
3. `docs/remediation/sql/english-demo-postflight.sql`

Stop immediately if any command exits nonzero or reports `BLOCKED`/`FAILED`.
Never edit the scripts after recording their hashes for the maintenance window.

## Required operator evidence

Before the maintenance window:

- Record the application release/commit currently running.
- Confirm the backend release contains the canonical-role compatibility change.
- Log in with a fresh demo session and confirm the authenticated role is
  `Admin`; stop if login, authorization, or role canonicalization fails.
- Record the target SQL Server and database names without recording credentials.
- Record the current `__EFMigrationsHistory`.
- Put the application into a no-write maintenance state.
- Create a full backup using the hosting control plane or an approved SQL Server
  backup mechanism.
- Verify the backup and perform a test restore into an isolated database.
- Record row counts and the payment total returned by the preflight.

If MonsterASP.NET cannot provide a verified backup/restore checkpoint, do not
run the updater.

## Artifact integrity

From the repository root, record the three hashes:

```powershell
Get-FileHash -Algorithm SHA256 `
  docs/remediation/sql/english-demo-preflight.sql, `
  backend/Padelito.Infrastructure/Data/Seed/english-demo-seed.sql, `
  docs/remediation/sql/english-demo-postflight.sql
```

Use a private operator environment for the SQL credentials. Do not place a
password in the command line, repository, transcript, or output file.

## Production execution

Run each command from the repository root with the explicitly reviewed server
and database. `SQLCMDPASSWORD` must come from the private operator environment.

```powershell
sqlcmd -S "<SERVER>" -d "<DATABASE>" -U "<USER>" -b -I `
  -i "docs/remediation/sql/english-demo-preflight.sql" `
  -o "english-demo-preflight-output.txt"
```

Open the output and require exactly one `READY` result. Review the database name,
counts, and payment total. If it is not the expected demo-only database, stop.

```powershell
sqlcmd -S "<SERVER>" -d "<DATABASE>" -U "<USER>" -b -I `
  -i "backend/Padelito.Infrastructure/Data/Seed/english-demo-seed.sql" `
  -o "english-demo-update-output.txt"
```

Require `UPDATED` and an English demo payment total of `109.88`.

```powershell
sqlcmd -S "<SERVER>" -d "<DATABASE>" -U "<USER>" -b -I `
  -i "docs/remediation/sql/english-demo-postflight.sql" `
  -o "english-demo-postflight-output.txt"
```

Require `PASS`, four demo people, four demo courts, five demo reservations,
three demo payments, and payment total `109.88`.

## Application smoke test

Before ending maintenance mode, verify:

- a new login session succeeds with the existing approved demo credential and
  receives the `Admin` role;
- club and administrator profile are in English;
- roles, reservation statuses, payment methods, and court types are in English;
- Maya, Ethan, Sofia, and Liam appear as the demo customers;
- Center Court, Grandstand, The Arena, and Match Point appear;
- Weekday Off-Peak appears;
- reservation, payment, dashboard, reporting, and audit pages load;
- demo amounts are presented as USD;
- application and SQL logs contain no new errors.

Do not create a real payment as a smoke test.

## Failure and recovery

The updater uses `XACT_ABORT`, a transaction, locking, fingerprint checks, and
postconditions. A SQL error rolls back the updater transaction.

If the updater succeeds but application verification fails:

1. Keep the application in maintenance mode.
2. Preserve all command output and application/SQL logs.
3. Restore the verified pre-change backup.
4. Re-run the read-only preflight against the restored database and confirm the
   original counts and total.
5. Escalate with the exact failed check; do not invent an inverse update.

## Completion record

Record:

- timestamp and operator;
- application release/commit;
- target server/database identifiers;
- backup and test-restore evidence;
- the three SHA-256 hashes;
- preflight, update, and postflight outputs;
- smoke-test results;
- time maintenance mode ended.
