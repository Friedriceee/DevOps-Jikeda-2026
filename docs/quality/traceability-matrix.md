# Proposal-to-implementation traceability matrix

Use this matrix as the single index from the approved proposal to executable
code, automated tests, and Sprint 4 evidence. Links point to repository paths;
the release owner adds CI/staging run URLs in the evidence column.

| Proposal requirement / acceptance claim | Implementation | Automated verification | Release evidence |
|---|---|---|---|
| Customers can register and sign in | `backend/src/TakeoutPlatform.Api/Features/Auth/`; `frontend/src/views/auth/` | `AuthEndpointTests.cs`, `AuthServiceTests.cs`, `RegisterView.spec.js`, `LoginView.spec.js` | CI test run + registration/login screenshot |
| Customers browse merchants and menus | `MerchantBrowseController.cs`, `DishController.cs`, `MerchantBrowseView.vue` | `DishEndpointTests.cs`, `MerchantBrowseView.spec.js` | staging smoke browse step + menu screenshot |
| Customers manage a cart with server-owned pricing | `CartController.cs`, `CartService.cs`, `PricingController.cs` | `CartEndpointTests.cs`, `CartServiceTests.cs`, `PricingEndpointTests.cs`, `PricingServiceTests.cs`, `cart.spec.js`, `pricing.spec.js` | cart/quote smoke output + UI evidence |
| Checkout rejects unauthorised roles and invalid addresses | `CheckoutController.cs`, authorization policies | `AuthorizationEndpointTests.cs`, `CheckoutEndpointTests.cs`, `CheckoutView.spec.js` | smoke `401`/`403` and validation steps |
| Payment failure rolls back stock/cart/order state | `CheckoutService.cs`, payment and inventory transaction code | `CheckoutEndpointTests.cs`, `CheckoutPostgresTests.cs` (PostgreSQL-only cases) | declined-payment smoke step + database consistency evidence |
| Payment is idempotent and server-priced | `CheckoutController.cs`, `CheckoutService.cs`, `Payments` migration | `CheckoutEndpointTests.cs`, `CheckoutPostgresTests.cs` | replay smoke step; payment/order count evidence |
| Multi-restaurant order waits for every merchant | `MerchantFulfillmentService.cs`, `RiderDeliveryService.cs` | `MerchantFulfillmentServiceTests.cs`, `RiderAssignmentChainTests.cs`, `RiderDeliveryServiceTests.cs` | two-merchant staging scenario or accepted-risk note |
| Merchant fulfilment supports accepted/preparing/ready/rejected states | `MerchantFulfillmentController.cs`, merchant UI | `MerchantFulfillmentEndpointTests.cs`, `MerchantFulfillmentServiceTests.cs`, `MerchantOrdersView.spec.js` | merchant demo screenshots and smoke output |
| Riders can accept, pick up, deliver, and complete ready orders | `RiderController.cs`, `RiderDeliveryService.cs`, `RiderDeliveryView.vue` | `RiderEndpointTests.cs`, `RiderAssignmentChainTests.cs`, `RiderDeliveryServiceTests.cs`, `delivery.spec.js` | end-to-end smoke output + rider screenshot |
| Administrators query accounts, coupons, orders, and audit logs | `AdminController.cs`, `AuditController.cs`, `AdminDashboardView.vue` | `AdminEndpointTests.cs`, `AdminServiceTests.cs`, `AuditEndpointTests.cs`, `AuditLogServiceTests.cs`, `AdminDashboardView.spec.js` | admin dashboard screenshot + CI result |
| Frontend is usable in English with protected routes and clear errors | `frontend/src/router/`, views, stores and API modules | `guards.spec.js`, `routes.spec.js`, auth/customer/merchant/rider view tests | keyboard/width checklist and screenshots |
| System is observable and deployable | `/api/health`, `/healthz`, Docker Compose, GitHub Actions | `HealthController.cs`, smoke script, CI, CodeQL | health/deep-link smoke output and green workflow checks |
| System meets the Sprint 4 performance target | k6 script `scripts/performance/sprint4-order-readiness.js` | 20-VU thresholds: p95 < 2s, failures <1%, checks >99% | attach k6 summary and environment/commit |

## Evidence rules

- Record the commit SHA, environment, date, VU count/duration, and test command
  beside every result.
- A green unit test run is not a substitute for the staging smoke test: the
  former checks isolated behavior, while the latter proves deployed wiring.
- Do not paste passwords, JWTs, or database connection strings into the report.
- If a PostgreSQL-only concurrency test is skipped, record that fact and run the
  staging smoke test before release; do not silently call it “passed”.
