# Sprint 4 performance test runbook

`scripts/performance/sprint4-order-readiness.js` is the repeatable 20-VU gate.
It is safe to run against a shared staging environment because its default
checkout request is an authenticated invalid-input probe and does not create an
order or change inventory.

## Run

```bash
API_URL=https://staging.example.test \
SMOKE_PASSWORD="$STAGING_SMOKE_PASSWORD" \
K6_VUS=20 \
K6_DURATION=30s \
k6 run scripts/performance/sprint4-order-readiness.js
```

The command fails if p95 is 2 seconds or slower, HTTP failures reach 1%, or
checks fall to 99% or below. Keep the k6 summary with the commit SHA and target
URL as release evidence.

## Quote and checkout coverage

The script always calls the server quote endpoint. An empty cart may return a
business `4xx` response; this is accepted in the generic shared-environment run
so that the test remains non-mutating. After preparing a disposable customer
cart, set `K6_REQUIRE_QUOTE=true` to require a successful `200` quote.

Successful checkout and the complete payment/order lifecycle are intentionally
covered by `scripts/smoke/staging-smoke.mjs`, which verifies payment rollback,
server-side pricing, idempotency, merchant fulfilment, rider delivery, and
customer/admin visibility. Run that smoke test before claiming release.
