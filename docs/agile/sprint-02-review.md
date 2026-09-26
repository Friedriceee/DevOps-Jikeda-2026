# Sprint 2 — Review

Reviewed against `main` at commit `080ac27` (26 Sep 2026, after PR #26 was merged).

## Outcome

| ID | Work item | Committed | State on `main` | Notes |
|---|---|---|---|---|
| US-09 | Customer registration and unified login | Yes | Done | `Features/Auth` (Account, `PasswordHasher`, JWT). Tests: `AuthServiceTests`, `AuthEndpointTests`. |
| SEC-01 | RBAC and resource ownership | Yes | Done | `[Authorize(Roles=…)]` on merchant, cart, address and order endpoints; identity from JWT `profile_id`. Tests: `AuthorizationEndpointTests` (401/403). |
| US-14 | Merchant and menu browsing | Yes | Done | `GET /api/merchants`, `MerchantBrowseView`, login/register pages, Pinia auth store, Axios token handling, route guards (PR #25, #26). |
| US-15A | Shopping-cart management | Yes | Done | Persistent server-side cart, grouped by merchant, prices and special-offer discounts computed by the backend (PR #26). |
| US-15B | Delivery address and order creation | Yes | Backend done; frontend partial | Address + pending-order creation in one transaction with inventory deduction (PR #24). No address/checkout page on `main` yet. |
| US-15C | View and cancel pending orders | Yes | Done | `GET /api/orders/user/{id}`, `DELETE /api/orders/{id}`; only `Pending` orders can be cancelled and inventory is restored. `OrderView` on the frontend. |
| INV-FIX | Dish soft deactivation (`IsActive`) | Yes | **Not on `main`** | Reported complete by the owner, but no `IsActive` exists on `main` or on any remote branch. Needs to be pushed and merged. |

Also delivered outside the Sprint 2 scope: **US-08 merchant registration** (merged 12 Sep, before the sprint started).

Committed 24 points; 21 points of work verified on `main`. INV-FIX (3 points) is not on `main`, so it is counted as carried over until merged.

## Quality and DevSecOps goals

| Goal | State |
|---|---|
| Authorization tests | Done — 401/403 and ownership cases in `AuthorizationEndpointTests` |
| Dependency scan and CodeQL | Done — `security` job and `codeql.yml` on every PR/push |
| PostgreSQL integration tests in CI | **Not on `main`** — `ci.yml` has no PostgreSQL service; backend tests use SQLite in-memory |
| Cross-module integration tests in CI | **Not on `main`** |
| Secret scanning in CI | **Not on `main`** — no scanning step in `ci.yml` |

## Verification (26 Sep 2026, merged `main`, run locally)

- Backend: `dotnet test` — **101 / 101 passed**
- Frontend: `vitest run` — **66 / 66 passed** (16 test files)
- Frontend: `npm run build` — passed
- GitHub Actions: CI and CodeQL green on `d48c32a` (before PR #26) and on the PR #26 head `f098a7d`

## Demo

Customer registers and logs in → browses merchants and menus → adds dishes to the cart → creates an address and a pending order → sees the order and cancels it. Address/checkout screens exist only in local work so far.

## Carried over to Sprint 3

- INV-FIX: push and merge `IsActive` soft deactivation, filter inactive dishes from the customer menu
- CI: PostgreSQL service + integration tests, cross-module tests, secret scanning
- Frontend: address selection and checkout pages
- US-10 (edit merchant profile), payment and discount at checkout, merchant fulfilment, rider delivery

## Metrics

- Pull requests merged during the sprint: #24 (address and orders), #25 (customer frontend), #26 (cart); authentication and RBAC and the registration frontend were merged directly (`9768df8`, `a4a3e29`)
- Tests on `main`: 101 backend, 66 frontend
- Reviews: PRs #24, #25 and #26 have no recorded reviewer approval (0 reviews on each); `main` branch protection has still not been confirmed — see the retrospective
