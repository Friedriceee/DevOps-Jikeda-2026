# User Stories and Acceptance Focus

The stories below use one ID vocabulary across the backlog, sprint records and
final traceability matrix. Detailed analysis and design documents currently
cover US-01 and US-02; the remaining complex stories should link to the final
report's diagrams and test evidence before submission.

## Merchant menu and offers

### US-01 — Create a dish

As a merchant, I want to create a dish so that customers can see it in my
menu and order it.

Acceptance focus: valid data is persisted, invalid data is rejected and the
new dish is visible in the merchant and customer menu views.

### US-02 — Manage dishes

As a merchant, I want to view, edit and deactivate dishes so that my menu and
inventory remain accurate.

Acceptance focus: only the owning merchant can change a dish; edits validate
price/category/inventory; deactivated dishes are not offered to customers.

### US-03 to US-07 — Manage special offers

As a merchant, I want to create, view, edit and delete special offers so that
eligible customers receive the intended promotion.

Acceptance focus: dates, thresholds, amounts and ownership are validated and
the API returns a consistent result shape.

## Identity and customer ordering

### US-08 and US-09 — Register and sign in

As a merchant or customer, I want to register and sign in securely so that my
role and resources are protected.

Acceptance focus: passwords are hashed, login returns a JWT, protected routes
redirect anonymous users, and wrong roles receive 403 rather than accessing a
resource.

### SEC-01 — Protect resources

As the platform, I want role and ownership checks on protected endpoints so
that one user cannot act on another user's, merchant's or rider's data.

Acceptance focus: anonymous requests return 401, wrong roles return 403, and
the server uses JWT identity rather than caller-supplied owner IDs.

### US-14 — Browse merchants and menus

As a customer, I want to browse merchants and active dishes so that I can
choose what to order.

Acceptance focus: available merchants and active dishes load through the API;
unavailable or inactive dishes are not selectable.

### US-15 — Manage cart and create an order

As a customer, I want to add dishes from one or more merchants, choose an
address and create an order so that the platform records my purchase.

Acceptance focus: cart quantities and merchant groups are preserved, the
server validates totals, address ownership is enforced, and pending-order
creation/cancellation keeps inventory consistent.

### US-15-PAY — Confirm payment

As a customer, I want to confirm a server-priced checkout so that payment and
order state are recorded exactly once.

Acceptance focus: declined payment rolls back, client-tampered totals are
ignored, and replaying an idempotency key does not create a second charge or
order.

## Fulfilment and administration

### US-16 — Merchant fulfilment

As a merchant, I want to process only my confirmed orders through `Accepted`,
`Preparing` and `ReadyForDelivery` so that riders receive an order only when
the food is ready.

Acceptance focus: ownership and transition rules are enforced; rejection
requires a reason and releases reserved inventory exactly once.

### US-17 — Rider delivery

As a rider, I want to accept an eligible ready order and update its delivery
status so that the customer can track completion.

Acceptance focus: eligibility filters apply, only one rider wins acceptance,
and the lifecycle progresses through `Assigned`, `PickedUp`, `Delivering` and
`Completed`.

### US-18 and US-19 — Administrator operations and audit

As an administrator, I want to manage accounts/coupons, query orders and
review audit events so that platform operations are controlled and traceable.

Acceptance focus: administrator-only access, filtered queries, immutable audit
events and clear English UI feedback.
