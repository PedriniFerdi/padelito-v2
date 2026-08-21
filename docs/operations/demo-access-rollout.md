# Demo access rollout

This runbook enables isolated, non-persistent public demo accounts and provisions
one real private administrator in the existing single-club MonsterASP.NET demo
database. It does not authorize remote execution without a verified backup and
an approved maintenance window.

## Security invariants

- `admin` and `juanperez` remain normal role-bearing users, but `Users.IsDemo`
  is set to `true` by the explicit provisioner.
- A demo JWT can only reach an isolated EF Core InMemory copy identified by the
  browser's `X-Demo-Session-Id` header. A missing or invalid header fails closed.
- `Ferdi` has `IsDemo=false`; its requests continue to use SQL Server.
- The private password must exist only in the hosting secret store during
  provisioning. Never put it in Git, application settings, logs, SQL scripts,
  command-line arguments, or support output.

## Required preparation

1. Confirm the deployed database is the dedicated demo database and contains
   exactly one active club.
2. Confirm active users named `admin` (Admin role) and `juanperez` (Reception
   role) already exist.
3. Create and verify a restorable database backup.
4. Put the application in maintenance mode.
5. Record the current release and migration history.

Run the privacy scripts in this exact order after maintenance mode is active
and before applying the EF migration:

1. `docs/operations/sql/demo-privacy-preflight.sql` must return `READY`.
2. `docs/operations/sql/demo-privacy-update.sql` must return `UPDATED`.
3. `docs/operations/sql/demo-privacy-postflight.sql` must return `PASS`.

The scripts are transactional, idempotent, and fail closed unless the dedicated
English demo database contains exactly the expected Ferdinando Perez customer
and Juan Perez Reception employee. They never contain the former private
customer identifier. Names and operational relationships are preserved while
both customer IDs, phone numbers, and email addresses are replaced with the
approved fictitious values.

## Schema and configuration

Apply migration `20260801181253_AddDemoUserFlag` using the reviewed deployment
procedure. The migration only adds `Users.IsDemo bit NOT NULL DEFAULT 0`.

Configure these values through MonsterASP.NET's private environment settings:

```text
DemoMode__Enabled=true
DemoMode__SessionIdleMinutes=30
DemoMode__MaxSessions=100
DemoAccessProvisioning__Enabled=true
DemoAccessProvisioning__PrivateUsername=Ferdi
DemoAccessProvisioning__PrivatePassword=<private secret>
DemoAccessProvisioning__AdminUsername=admin
DemoAccessProvisioning__ReceptionUsername=juanperez
DemoAccessProvisioning__ReceptionPersonId=3
DemoAccessProvisioning__ReceptionFirstName=Juan
DemoAccessProvisioning__ReceptionLastName=Perez
DemoAccessProvisioning__ReceptionPasswordHash=<temporary private hash, only when creating the account>
```

The provisioner's non-sensitive profile defaults are `Ferdi Admin`, customer
ID `99000001`, phone `+1 555 010 0001`, and
`ferdi.admin@padelito.example`. Override the corresponding
`DemoAccessProvisioning__FirstName`, `LastName`, `Dni`, `Phone`, or `Email`
setting before startup if any value conflicts with existing data.

Rotate `Jwt__Key` in the same maintenance window so every old cookie is
invalidated before public traffic resumes.

## Provision and verify

1. Deploy the new artifact and start it once. Startup must fail if the database
   topology, public accounts, roles, private profile, or password do not match.
2. Confirm logs report provisioning completion without printing a password.
3. Log in as `admin`; confirm `Admin · Demo`, create a temporary customer, use
   it in a reservation and payment, and confirm dashboard/report/audit changes.
4. Reload; confirm all temporary changes disappear.
5. Repeat a write with `juanperez` and confirm it disappears after reload.
6. Log in privately as `Ferdi`, perform an approved reversible test change, and
   confirm it remains after reload.
7. Query the database through an approved private channel and verify only
   `admin` and `juanperez` have `IsDemo=1`; `Ferdi` must have `IsDemo=0`.

After verification, set `DemoAccessProvisioning__Enabled=false`, remove
`DemoAccessProvisioning__PrivatePassword` and
`DemoAccessProvisioning__ReceptionPasswordHash` from the hosting secret store,
and restart. Keep `DemoMode__Enabled=true`.

The generated private credential is delivered outside Git at
`C:\Users\Ferdinando\Desktop\Padelito-admin-privado.txt`. Restrict the file ACL
to the current Windows user and never copy its password into deployment logs or
the rollout record.

## Rollback

Do not reopen public traffic on an older application version: it does not route
demo accounts away from SQL Server. During rollback, keep maintenance mode or
deactivate both public accounts. Do not drop `IsDemo` while their credentials
remain published. Restore the verified backup if provisioning changed data but
the release cannot be completed safely.
