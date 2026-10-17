# Sprint 4 review

## Review objective

Demonstrate the integrated customer → payment → merchant → rider → customer
order lifecycle, the administrator/audit surfaces, and the release gates.

## Delivered scope to demonstrate

- Server-priced checkout with payment persistence, idempotency, and rollback.
- Multi-restaurant fulfilment that waits for every merchant before rider offer.
- Rider assignment and delivery tracking through `Completed`.
- Administrator account/coupon/order/audit views.
- English customer, merchant, rider, and checkout interfaces.
- Backend/frontend regression suites, HTTP smoke test, and 20-VU k6 gate.

## Evidence to attach

| Item | Evidence link/result |
|---|---|
| Backend tests | fill after the final CI run |
| Frontend tests/build | fill after the final CI run |
| Staging smoke | fill with target URL, commit SHA, and terminal output |
| k6 performance | fill with VUs, duration, p95, failure/check rates |
| Security checks | fill with CodeQL/SCA/secret/image scan results |
| UI/demo screenshots | fill with the screenshot set from `docs/demo/sprint-04-demo-script.md` |

## Acceptance decision

The reviewer records **Accepted**, **Accepted with explicitly documented risk**,
or **Not accepted**. A green unit-test run alone is not sufficient for
acceptance; the staging smoke and release-blocking rules in
`docs/quality/sprint-04-quality-gates.md` also apply.
