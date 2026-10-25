# Sprint 3 — Plan

## Sprint goal

Extend the customer pre-checkout flow into a complete order lifecycle:
server-priced checkout and simulated payment, merchant fulfilment, rider
assignment and delivery completion. Establish a repeatable container and
staging delivery path early enough to expose integration problems.

## Planned workstreams

| Workstream | Acceptance focus | Owner |
|---|---|---|
| Checkout and payment | Server-owned total, declined-payment rollback and idempotent replay | member1 |
| Pricing and checkout UI | Discount Strategy, coupon eligibility and English price breakdown | member2 |
| Menu Factory and inventory | Factory Method boundary, reservation/release and no negative stock | member3 |
| Merchant fulfilment | Ownership checks and Confirmed → Accepted → Preparing → ReadyForDelivery | member4 |
| Rider assignment and delivery | Eligibility chain, one-winner acceptance and Assigned → Completed flow | member5 |
| Integration and CD | Container build, health checks, smoke test and rollback path | Team/member1 lead |

## Shared contracts to freeze before coding

- Order and fulfilment state names.
- Server-generated pricing result.
- Inventory reservation and exactly-once release behavior.
- Rider eligibility fields and delivery status transitions.
- Environment variables, demo accounts and health-check paths.

## Definition of done

- One order can pass checkout, payment, merchant fulfilment and rider delivery.
- Failure, authorization, idempotency and concurrency cases have tests.
- Frontend routes expose the customer, merchant and rider slices in English.
- The container stack starts from documented configuration and exposes health
  checks.
- Each story has a reviewed PR and linked test or demo evidence.

## Scope control

No real payment gateway, GPS navigation, native mobile application or
multi-region production deployment is included. Administrator/audit closure,
final performance/security evidence and final documentation are carried into
Sprint 4.
