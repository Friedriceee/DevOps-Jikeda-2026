# Sprint 2 — Retrospective

## What went well

- The team extended the Sprint 1 structure without changing the overall
  frontend, API and PostgreSQL architecture.
- JWT identity, route guards and ownership checks made authorization behavior
  explicit and testable.
- The cart, address and order work was delivered as one customer-facing slice.
- PostgreSQL integration and authorization checks exposed cross-module issues
  before the sprint was closed.

## What could be improved

- Some frontend and backend work started before all API contracts were frozen,
  which created integration rework.
- Cross-module and CI verification began too late in the sprint.
- The next sprint needed an agreed order-state vocabulary before payment and
  fulfilment work started.

## Actions for Sprint 3

| Action | Owner | Evidence |
|---|---|---|
| Freeze shared pricing, order-state and inventory contracts before feature work | Team | Sprint 3 plan and API contract |
| Run PostgreSQL and cross-module checks at the midpoint | Team | CI run and integration test result |
| Keep every feature as a vertical slice with tests and a reviewed PR | Team | PR history and CI checks |
