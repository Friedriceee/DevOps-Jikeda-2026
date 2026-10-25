# Sprint 4 — Plan

## Sprint goal

Turn the integrated order lifecycle into a release-ready demonstration and
submission package. The sprint closes the administrator/audit surfaces,
unifies order and inventory behavior across roles, and produces repeatable
quality and delivery evidence.

## Planned scope

| Workstream | Acceptance focus | Owner |
|---|---|---|
| Checkout, payment and CD integration | Server-priced checkout, idempotency, image build/scan and release path | member1 |
| Administrator backend | Accounts, roles, coupons and order queries with RBAC | member2 |
| Administrator UI and audit | English dashboard, audit event recording and filtering | member3 |
| Order lifecycle consistency | State vocabulary, cancellation/rejection, inventory compensation and concurrency | member4 |
| Quality gates and final delivery | Traceability, regression, performance, security, accessibility, report and demo | member5 |

## Release gates

- Backend, frontend, integration and smoke tests pass.
- Anonymous and wrong-role requests receive the expected 401/403 results.
- Declined payment, duplicate requests, rejection and cancellation preserve
  order, payment and inventory consistency.
- The k6 gate meets the agreed p95, failure-rate and check-rate thresholds.
- CodeQL, dependency, secret and image scans have no untriaged release-blocking
  finding.
- Health checks and SPA deep links work in the selected runtime environment.
- The final report, presentation and Agile package use the same terminology and
  evidence commit.

## Planned sequence

1. Synchronize main and merge the approved Sprint 3 carry-over.
2. Freeze status, administrator DTO and audit-event contracts.
3. Build administrator and lifecycle slices with migrations and tests.
4. Run full regression, security, performance and smoke gates.
5. Rehearse the final demo, update report evidence and tag the release.

## Open dependency

Persistent staging and rollback evidence depend on an available host and
configured deployment secrets. If those are unavailable, the final report must
label local or ephemeral rehearsal evidence explicitly and record the accepted
risk rather than calling it persistent staging.
