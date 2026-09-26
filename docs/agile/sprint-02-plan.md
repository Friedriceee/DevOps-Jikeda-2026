# Sprint 2 — Plan

- Period: 15 Sep 2026 – 26 Sep 2026
- Sprint goal: establish secure authentication and role-based access control, and deliver the customer pre-checkout flow (browse merchants and menus → shopping cart → delivery address → pending order → view/cancel order).
- Planned size: 24 story points (7 work items).

## Scope

| ID | Work item | Points | Owner |
|---|---|---|---|
| US-09 | Customer registration and unified login (Account, password hashing, JWT) | 5 | WANG Qinyang |
| SEC-01 | RBAC and resource-ownership control (JWT claims, 401/403) | 3 | WANG Qinyang |
| US-14 | Merchant and menu browsing (frontend login/session/route guards + browse UI) | 5 | HAO Mingyang |
| INV-FIX | Dish soft deactivation (`IsActive`) and customer-menu filtering | 3 | TANG Jinfan |
| US-15A | Shopping-cart management (per-merchant grouping, server-side pricing) | — | ZHAO Haotian |
| US-15B | Delivery address and order creation (Order Builder, transaction, inventory) | — | LIU Tongyu |
| US-15C | View and cancel pending orders | — | LIU Tongyu |

US-15A/B/C together make up US-15 (8 points in the [product backlog](product-backlog.md)); the split between the three parts was not recorded when the sprint was planned. INV-FIX's 3 points are derived from the sprint total (24 − 5 − 3 − 5 − 8).

Out of scope (reserved for later sprints): payment, discount calculation at checkout, merchant fulfilment, rider delivery, staging deployment.

## Quality and DevSecOps goals

- Authorization tests (401/403, ownership) for every protected endpoint
- PostgreSQL integration testing and cross-module integration tests in CI
- Secret scanning in CI, in addition to the existing dependency scan and CodeQL
- Every change lands through a pull request with CI green

## Workflow

Each story is delivered as a vertical slice: analysis and design → implementation → tests → pull request → CI verification → merge to `main`. API contracts and ownership rules are recorded in [api-contract.md](../api-contract.md) and each authenticated endpoint takes its identity from the JWT `profile_id` claim, never from a client-supplied ID.

## Definition of Done

- [x] Customer can register and log in; token is attached to protected calls; route guards enforce role access
- [x] Protected endpoints return 401 without a token and 403 for the wrong role
- [x] Customer can browse merchants and menus
- [x] Customer can add, update and remove cart items; totals are computed by the backend
- [x] Customer can create addresses and a pending order in one transaction; inventory is deducted
- [x] Customer can list own orders and cancel a pending order; inventory is restored
- [ ] Dish soft deactivation (`IsActive`) merged to `main` — see [sprint-02-review.md](sprint-02-review.md)
- [ ] PostgreSQL integration tests and secret scanning running in CI on `main` — see [sprint-02-review.md](sprint-02-review.md)
- [ ] Customer address / checkout pages merged to `main` — see [sprint-02-review.md](sprint-02-review.md)
