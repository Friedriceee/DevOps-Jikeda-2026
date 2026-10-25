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
| HTTP smoke | Latest local main run passed 20/20 PostgreSQL-backed steps against the local API/frontend; attach the terminal output and commit SHA |
| k6 performance | Attach the required final 20-VU/30-second run. A separate 10-VU/10-second diagnostic run passed all checks locally; do not label it as the final gate |
| Security checks | PR #38 CodeQL C#/JavaScript, security, and Trivy filesystem checks passed; npm audit has 0 high/critical findings |
| UI/demo screenshots | Existing figures 1–17 retained; execution evidence and auditable links added to `Team3_Sprint4_Member3.docx` |

## Acceptance decision

The local release evidence is ready for review. The reviewer still records
**Accepted**, **Accepted with explicitly documented risk**, or **Not accepted**
after the final CI/CD and staging evidence is attached. Local or ephemeral runs
must not be labelled as persistent staging evidence.
