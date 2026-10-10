# Sprint 3 — Member 3 handoff

Scope: Menu Factory (Standard, Bundle, Promotional), availability, atomic stock reservation,
release protection, migration and tests. All new menu UI labels and menu validation messages
are English. Pricing/Strategy, payment, rider delivery, merchant state transitions and CI/CD
configuration are unchanged. The only shared-service changes are inventory integration points.

## Verification on 10 October 2026

- Backend: **190 passed, zero failed/skipped** with PostgreSQL configured. Of these,
  **41 added Member 3 tests** cover factories, validation/API behavior, inventory, integration
  and PostgreSQL (5 tests). The remainder are existing regression tests, not personal contributions.
- Frontend: **99 passed across 24 files**; **11 added tests** cover menu settings, English
  menu management, authenticated merchant selection and unavailable customer menu items.
- Backend build and frontend production build succeed. The existing large-JS-chunk warning remains.
- Actual HTTP smoke test: stock **8 → 6 → 8 → 8** for create, cancel and repeated cancel;
  excessive quantity and expired promotion are rejected without changing stock.
- PostgreSQL tests: 10 concurrent orders compete for 3 units; only 3 succeed. Concurrent identical
  reservations deduct once; changed replay rolls back; concurrent releases restore once.
- Member 3 migration: up/down/up preserves legacy Standard dish names and quantities.

## Reproduce tests

```sh
dotnet test backend/TakeoutPlatform.sln
cd frontend
npm ci
npm run test
npm run build
```

The default backend run skips the 5 PostgreSQL tests when `JIKEDA_TEST_POSTGRES` is absent.
To execute them, set this variable to a **local disposable PostgreSQL admin connection** and
run `dotnet test` again. The fixture refuses non-local hosts, creates unique
`jikeda_inventory_test_*` databases and removes only its own databases afterward.
The fixture account needs CREATE DATABASE permission. Never point this workflow at production.
CI's existing SQLite-only setup is unchanged; PostgreSQL results above are local evidence,
not a claim that these 5 tests run in GitHub Actions.

## Local migration and demo

Apply `20261010090352_AddMenuFactoryAndInventory` with `dotnet ef database update` to a
disposable database. Do not silently clamp existing negative stock: the new constraint must fail
until those data issues are explicitly resolved. The subsequent existing rider migration is
outside Member 3 scope. A full-chain attempt fails at `202610110001_AddRiderDelivery`:
its seed-data operation cannot map `Riders` without target metadata or explicit column types.
This is a pre-existing main-branch migration problem; it has been verified, not repaired here.
The demo database remains at the successful Member 3 migration. Do not claim that the full
team migration chain or production deployment passes.

`scripts/member3-inventory-smoke.mjs` registers local-only demo accounts, creates/reuses four
English demo items, and exercises order creation/cancellation. Set `JIKEDA_DEMO_PASSWORD`
and optionally `JIKEDA_SMOKE_OUTPUT`. It never prints credentials or tokens and refuses remote hosts.
It expects the demo Standard item's initial stock to be 8; it does not reset real inventory.
Its disposal scope is only the named local test databases/accounts, not the team's database.

## Figure mapping

- **Figure 3:** `docs/diagrams/member3-menu-inventory.puml` (Factory Method creator/product
  hierarchy, persistent dish metadata and transactional reservation ledger).
- **Figure 9:** English Menu Management showing all three types, quantities and availability.
  Capture a second customer-menu view with the expired item disabled. Use the actual stock
  8 → 6 → 8 screenshots if the report needs direct reservation/release evidence.
- **Test evidence / Figure 18:** capture actual test output, including named PostgreSQL tests;
  include the environment and distinguish the 41/11 added tests from aggregate suite totals.
- **PR evidence:** capture the PR commit list and checks only after GitHub has run them.
  Local passes do not establish review approval, a merge, or successful deployment.

## Report-ready Member 3 contribution

Implemented the Menu Factory using concrete creators for Standard, Bundle and Promotional items,
with type-specific validation and UTC promotion availability. Added transactional inventory
reservations and merchant-scoped releases, conditional stock updates and repeated-release protection.
Integrated inventory calls with existing order creation/cancellation and merchant rejection without
changing their other business rules. Delivered English menu management and availability displays,
a backward-compatible menu migration, 41 backend and 11 frontend tests, and real PostgreSQL
concurrency/migration verification. Implementation and local verification are complete;
review, merge and deployment must be reported separately using their actual status.
