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
| Backend tests | 259 passed, 10 skipped, 0 failed; local Release run and CI `backend` job passed |
| Frontend tests/build | 126 tests across 28 files passed; Vite production build passed; CI `frontend` job passed |
| HTTP smoke | 19/19 local PostgreSQL-backed steps passed at `http://127.0.0.1:5080` with frontend health/deep-link checks |
| k6 performance | 20 VUs for 30 seconds; 19,042 requests; p95 7.13 ms; failed requests 0.00%; checks 100.00% |
| Security checks | PR #38 CodeQL C#/JavaScript, security, and Trivy filesystem checks passed; npm audit has 0 high/critical findings |
| UI/demo screenshots | Existing figures 1–17 retained; execution evidence and auditable links added to `Team3_Sprint4_Member3.docx` |

## Acceptance decision

The local release evidence is complete for review. The reviewer still records
**Accepted**, **Accepted with explicitly documented risk**, or **Not accepted**
after staging deployment/CD evidence is available. The local run is not labelled
as staging evidence.
