# Sprint 3 — Review

## Review objective

Show the transition from a customer pre-checkout system to an integrated
checkout-to-delivery lifecycle.

## Delivered scope

- Server-owned pricing and discount strategies.
- Simulated approved/declined payment with rollback and idempotent replay.
- Inventory reservation and release rules.
- Menu Factory Method and inventory-related persistence changes.
- Authenticated merchant queue and guarded fulfilment transitions.
- Rider eligibility chain, concurrency-safe acceptance and delivery states.
- English customer checkout, merchant order and rider delivery screens.
- Containerized CI/full-stack verification and a repeatable smoke path.

## Review evidence

The Sprint 3 progress report records the workstream ownership, design figures,
module-level test evidence and the Docker Compose smoke path. The repository
keeps the corresponding service tests, frontend tests, smoke script and
deployment design.

## Acceptance decision

The feature work was accepted as the Sprint 4 starting baseline. Staging
availability, final release hardening, administrator/audit closure and final
evidence remained explicit Sprint 4 items where the environment was not yet
available.

## Carry-forward to Sprint 4

- Administrator accounts, coupons, orders and audit logs.
- Unified cross-role state and cancellation/rejection consistency.
- Release-blocking security, regression and performance evidence.
- Final report, presentation and repeatable release/rollback evidence.
