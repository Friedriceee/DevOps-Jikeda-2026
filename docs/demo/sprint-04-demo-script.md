# Sprint 4 final demo script

Use the same script against staging and the local Docker-backed environment.
All credentials come from the environment or the approved demo-account sheet;
do not place passwords in screenshots or commit them.

## Before the demo

1. Confirm the target commit SHA and that `/api/health` returns `status: ok`.
2. Confirm the frontend shell and `/customer/checkout` deep link load.
3. Run backend tests, frontend tests/build, the staging smoke test, and the k6
   gate. Save their terminal summaries.
4. Seed or verify at least two merchants, a customer address, menu inventory,
   one administrator, one merchant per restaurant, and one rider.

## Walkthrough

1. **Customer** — sign in, browse two merchants, open menus, add dishes to the
   cart, show the server quote/discount, and open checkout.
2. **Checkout controls** — show that anonymous checkout is `401`, a wrong role
   is `403`, the server ignores a tampered total, and a declined payment leaves
   cart/stock unchanged.
3. **Merchant** — sign in as each merchant involved in a multi-restaurant order;
   move each fulfilment through `Accepted`, `Preparing`, and
   `ReadyForDelivery`. Explain that the rider offer waits for all merchants.
4. **Rider** — accept the now-ready delivery, then show `PickedUp`,
   `Delivering`, and `Completed`.
5. **Administrator/customer** — show the final order in administrator query,
   audit logs where applicable, and customer order history.
6. **Engineering evidence** — show the traceability matrix, green CI checks,
   CodeQL/security results, and the k6 p95 summary.

## Screenshot checklist

- Customer merchant/menu and cart with server-calculated price.
- Checkout validation or declined-payment result without exposing secrets.
- Both merchant queues showing fulfilment progress.
- Rider delivery state and final completed status.
- Administrator order/audit view and customer history.
- Terminal summaries for backend/frontend tests, staging smoke, and k6.

## Fallback

If a live staging dependency is unavailable, stop at the failing gate, capture
the error and commit SHA, and label the screenshot “blocked — not release
evidence”. Do not replace an integration result with a unit-test screenshot.
