# Proposal-to-implementation traceability matrix

Use this matrix as the single index from the approved proposal to executable
code, automated tests, and Sprint 4 evidence. The 2026-10-17 local evidence was
run at commit `9892f2e` against PostgreSQL, with the final CI links recorded in
the report. Staging/CD evidence is still environment-dependent.

| Proposal requirement / acceptance claim | Implementation | Automated verification | Release evidence |
|---|---|---|---|
| Customers can register and sign in | `backend/src/TakeoutPlatform.Api/Features/Auth/`; `frontend/src/views/auth/` | `AuthEndpointTests.cs`, `AuthServiceTests.cs`, `RegisterView.spec.js`, `LoginView.spec.js` | 19-step smoke login checks + CI `frontend`/`backend` jobs |
| Customers browse merchants and menus | `MerchantBrowseController.cs`, `DishController.cs`, `MerchantBrowseView.vue` | `DishEndpointTests.cs`, `MerchantBrowseView.spec.js` | Local smoke browse/menu step passed; menu screenshot retained in report |
| Customers manage a cart with server-owned pricing | `CartController.cs`, `CartService.cs`, `PricingController.cs` | `CartEndpointTests.cs`, `CartServiceTests.cs`, `PricingEndpointTests.cs`, `PricingServiceTests.cs`, `cart.spec.js`, `pricing.spec.js` | Local smoke cart/quote steps passed + UI evidence |
| Checkout rejects unauthorised roles and invalid addresses | `CheckoutController.cs`, authorization policies | `AuthorizationEndpointTests.cs`, `CheckoutEndpointTests.cs`, `CheckoutView.spec.js` | Local smoke `401`/`403` and k6 safe validation probe passed |
| Payment failure rolls back stock/cart/order state | `CheckoutService.cs`, payment and inventory transaction code | `CheckoutEndpointTests.cs`, `CheckoutPostgresTests.cs` (PostgreSQL-only cases) | Local smoke declined-payment step passed without stock/cart change |
| Payment is idempotent and server-priced | `CheckoutController.cs`, `CheckoutService.cs`, `Payments` migration | `CheckoutEndpointTests.cs`, `CheckoutPostgresTests.cs` | Local smoke server-pricing and replay steps passed; order #6 completed |
| Multi-restaurant order waits for every merchant | `MerchantFulfillmentService.cs`, `RiderDeliveryService.cs` | `MerchantFulfillmentServiceTests.cs`, `RiderAssignmentChainTests.cs`, `RiderDeliveryServiceTests.cs` | Automated service/assignment tests passed; two-merchant staging evidence remains pending |
| Merchant fulfilment supports accepted/preparing/ready/rejected states | `MerchantFulfillmentController.cs`, merchant UI | `MerchantFulfillmentEndpointTests.cs`, `MerchantFulfillmentServiceTests.cs`, `MerchantOrdersView.spec.js` | Local smoke merchant Accept/StartPreparing/MarkReady steps passed |
| Riders can accept, pick up, deliver, and complete ready orders | `RiderController.cs`, `RiderDeliveryService.cs`, `RiderDeliveryView.vue` | `RiderEndpointTests.cs`, `RiderAssignmentChainTests.cs`, `RiderDeliveryServiceTests.cs`, `delivery.spec.js` | Local smoke rider lifecycle passed + rider screenshot |
| Administrators query accounts, coupons, orders, and audit logs | `AdminController.cs`, `AuditController.cs`, `AdminDashboardView.vue` | `AdminEndpointTests.cs`, `AdminServiceTests.cs`, `AuditEndpointTests.cs`, `AuditLogServiceTests.cs`, `AdminDashboardView.spec.js` | Local smoke admin paid/completed visibility steps passed + screenshots |
| Frontend is usable in English with protected routes and clear errors | `frontend/src/router/`, views, stores and API modules | `guards.spec.js`, `routes.spec.js`, auth/customer/merchant/rider view tests | keyboard/width checklist and screenshots |
| System is observable and deployable | `/api/health`, `/healthz`, Docker Compose, GitHub Actions | `HealthController.cs`, smoke script, CI, CodeQL | Local health/deep-link checks passed; PR #38 CI/CodeQL/Trivy checks green |
| System meets the Sprint 4 performance target | k6 script `scripts/performance/sprint4-order-readiness.js` | 20-VU thresholds: p95 < 2s, failures <1%, checks >99% | 20 VUs/30s, 19,042 requests, p95 7.13 ms, failures 0.00%, checks 100.00%, commit `9892f2e` |

## Evidence rules

- Record the commit SHA, environment, date, VU count/duration, and test command
  beside every result.
- A green unit test run is not a substitute for the staging smoke test: the
  former checks isolated behavior, while the latter proves deployed wiring.
- Do not paste passwords, JWTs, or database connection strings into the report.
- If a PostgreSQL-only concurrency test is skipped, record that fact and run the
  staging smoke test before release; do not silently call it “passed”.
