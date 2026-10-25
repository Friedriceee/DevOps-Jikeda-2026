# Sprint 4 — Burndown and Delivery Ledger

Sprint 4 closes the release and submission work rather than introducing an
unrelated feature set. Replace the status values below with the final team
board state and attach the corresponding CI/deployment links at release time.

| Work item | Owner | Evidence | Status |
|---|---|---|---|
| Checkout/payment and CD carry-over | member1 | backend tests, smoke and CD workflow | Completed in current main; final deployment status to confirm |
| Administrator accounts, coupons and order queries | member2 | admin tests and UI evidence | Completed in current main; final CI link to attach |
| Administrator UI and audit log | member3 | frontend/admin/audit tests and screenshots | Completed in current main; final CI link to attach |
| Order lifecycle and inventory consistency | member4 | transition, rollback and concurrency tests | Completed in current main; final integration evidence to attach |
| Regression, security, performance and final demo package | member5 | quality gates, traceability matrix and demo evidence | Local evidence complete; persistent staging/CD evidence to confirm |

## Current local evidence

- Latest local main commit: `41d88ed`.
- PostgreSQL-backed HTTP smoke: 20/20 steps passed against the local API and
  frontend; the test drove one order from checkout through rider completion.
- A diagnostic k6 run with 10 VUs for 10 seconds passed all checks, with
  `p(95) < 2000 ms` and zero failed requests. The required final 20-VU/30-second
  result must be attached from the release run if it is not already recorded
  in the report.

## Release close-out

- [ ] Insert the final GitHub Project burndown chart.
- [ ] Link the final backend/frontend/stack/security/CodeQL CI runs.
- [ ] Link persistent staging or clearly label local/ephemeral evidence.
- [ ] Record the release tag and accepted open risks.
