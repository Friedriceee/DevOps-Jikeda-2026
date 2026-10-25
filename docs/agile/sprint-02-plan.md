# Sprint 2 — Plan

## Sprint goal

Extend the Sprint 1 merchant foundation into a secure customer pre-checkout
vertical slice. A customer should be able to register and sign in, browse
merchants and menus, manage a cart, save an address, create a pending order,
and review or cancel an eligible pending order.

## Planned scope

| Work item | Acceptance focus | Status |
|---|---|---|
| US-08 Merchant registration | Registration persists a merchant account and profile | Completed |
| US-09 Customer registration and login | Password hashing, JWT login and session handling | Completed |
| SEC-01 Authentication and RBAC | Protected endpoints return the correct 401/403 result and enforce resource ownership | Completed |
| US-14 Merchant and menu browsing | Customer sees active merchants and available dishes | Completed |
| US-15A Cart management | Add, update and remove items; group items by merchant | Completed |
| US-15B Address and order creation | Address ownership, validated pending order and consistent inventory update | Completed |
| US-15C Pending-order review and cancellation | Customer sees own orders and can cancel only eligible pending orders | Completed |
| Quality and integration | PostgreSQL integration, cross-module checks, secret scanning and PR verification | Completed |

## Planned working approach

The team used a vertical slice for each customer workflow: agree the API
contract, implement the service and persistence change, connect the Vue page,
add backend/frontend tests, then merge through a reviewed pull request.

## Definition of done

- Customer and merchant registration/login work against PostgreSQL.
- JWT claims, route guards and ownership checks protect customer resources.
- Cart totals are calculated and validated by the server.
- Address, order and inventory changes are transactionally consistent.
- Backend, frontend and authorization tests pass in CI.
- The report records any PostgreSQL-only or security checks that were skipped.

## Carry-forward to Sprint 3

Payment, server-side discount calculation, merchant fulfilment, rider delivery
and staging deployment were intentionally left for the next iteration.
