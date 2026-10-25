# Sprint 2 — Review

## Review objective

Demonstrate the secure customer pre-checkout flow built on the Sprint 1
merchant foundation.

## Delivered scope

- Customer and merchant registration with password hashing and JWT login.
- Role-based authorization and resource ownership checks.
- Customer merchant/menu browsing and active-dish filtering.
- Persistent cart management with merchant grouping and server-owned totals.
- Delivery-address creation and retrieval.
- Pending-order creation, order queries and eligible cancellation.
- PostgreSQL integration, cross-module verification and secret scanning in CI.

## Review evidence

The Sprint 2 progress report contains the customer login, merchant browsing,
cart, checkout and order-history figures, together with the sprint burndown.
Executable coverage is maintained in the backend and frontend test suites and
in the authorization and integration workflow.

## Acceptance decision

The planned Sprint 2 scope was completed and carried into Sprint 3 as the
customer pre-checkout baseline. Payment, fulfilment, rider delivery and
staging remained explicit follow-on work rather than unrecorded omissions.

## Items carried forward

- Server-calculated pricing and promotion eligibility.
- Simulated payment, rollback and idempotency.
- Merchant fulfilment states and inventory release.
- Rider assignment and delivery tracking.
- Repeatable container deployment and smoke verification.
