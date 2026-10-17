# Sprint 4 quality gates and release checklist

This document is the member5 release gate for Sprint 4. It turns the proposal
requirements into repeatable checks and links each check to executable evidence.
It is intentionally separate from the formal report template so that the same
commands can be used locally, in CI, and during the staging demo.

## Release-blocking rules

The release is blocked when any of the following is true:

1. Backend, frontend, integration, or smoke tests fail.
2. An anonymous request is accepted where `401` is required, or a wrong-role
   request is accepted where `403` is required.
3. A declined payment changes stock, cart, payment, or order state; a repeated
   idempotency key creates a second charge/order; or a multi-restaurant order is
   offered to a rider before every merchant is ready.
4. The k6 performance gate reports `p(95) >= 2000 ms`, request failures of 1% or
   more, or check success of 99% or less.
5. CodeQL, dependency scanning, secret scanning, or the image scan reports an
   untriaged Critical/High finding.
6. The staging health check, SPA deep link, migration, or required demo account
   is unavailable.
7. A required form cannot be reached by keyboard, has no useful label, or shows
   an unclear error at a common desktop/mobile width.

## Reproducible checks

Run these commands from the repository root. Keep passwords in the shell
environment or a CI secret; never put them in a document or commit.

```bash
# Backend unit/integration suite (SQLite tests; ten PostgreSQL concurrency tests
# are skipped unless JIKEDA_TEST_POSTGRES is configured).
dotnet test backend/TakeoutPlatform.sln -c Release -m:1

# Frontend regression and production build.
npm ci --prefix frontend --no-audit --no-fund
npm test --prefix frontend -- --run
npm run build --prefix frontend

# Full HTTP happy/failure path against local or staging.
BASE_URL=http://localhost:5173 \
API_URL=http://localhost:5080 \
SMOKE_PASSWORD="$SMOKE_PASSWORD" \
node scripts/smoke/staging-smoke.mjs

# Twenty-VU read/validation pressure gate (requires k6 installed).
API_URL=http://localhost:5080 \
SMOKE_PASSWORD="$SMOKE_PASSWORD" \
k6 run scripts/performance/sprint4-order-readiness.js
```

The k6 script covers health, merchant/menu, cart, quote, customer order query,
and an authenticated checkout validation probe with 20 concurrent VUs. It does
not create orders or consume inventory. The successful checkout, payment
rollback, idempotency, merchant fulfilment, rider delivery, and final customer
history checks are intentionally covered by the dependency-free staging smoke
test. Set `K6_REQUIRE_QUOTE=true` only after the demo customer has a prepared
cart; otherwise an empty cart is a valid non-mutating quote response.

## Evidence ledger

| Gate | Primary evidence | Owner/status to record at release |
|---|---|---|
| Backend tests | `backend/tests/TakeoutPlatform.Api.Tests/` and CI `backend` job | attach CI run URL and pass/skip counts |
| Frontend tests/build | `frontend/src/**/__tests__/`, `npm run build` | attach CI run URL and pass count |
| Full order lifecycle | `scripts/smoke/staging-smoke.mjs` | attach terminal output and staging URL |
| 401/403 and rollback | smoke output plus `AuthorizationEndpointTests.cs`, `CheckoutEndpointTests.cs` | attach test run |
| 20-VU performance | `scripts/performance/sprint4-order-readiness.js` | attach k6 summary; record VUs, duration, p95 |
| Security | `.github/workflows/ci.yml`, `.github/workflows/codeql.yml`, Dependabot, secret scan, image scan | attach green checks or triage reference |
| Accessibility | customer, merchant, rider and admin pages | attach keyboard/width screenshots and note any exception |
| Delivery package | traceability matrix, demo script, review, retro, burndown | link the final report and presentation |

## Definition of done

Member5 work is complete when the matrix and evidence ledger are updated with
real run links/results, all release-blocking rules are green or have an
explicitly accepted risk, and the final report/demo use the same terminology as
the API and UI (English labels, `Paid → Accepted → Preparing → ReadyForDelivery
→ Assigned → PickedUp → Delivering → Completed`).
