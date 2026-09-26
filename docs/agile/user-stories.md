# User Stories

模板：

> 作为 **<角色>**，我想 **<目标>**，以便 **<价值>**。
> 验收标准：……

范围内 story 的详细分析与设计放 [../analysis-design/](../analysis-design/)。

## Sprint 1

### US-01 商家创建菜品
> 作为商家，我想新增一道菜品，以便顾客能在我的店里看到并下单。

详见 [../analysis-design/US-01-merchant-create-dish.md](../analysis-design/US-01-merchant-create-dish.md)。

### US-02 商家查看、编辑和下架菜品
> 作为商家，我想查看、编辑和下架我的菜品，以便及时维护菜单内容。

详见 [../analysis-design/US-02-merchant-manage-dish.md](../analysis-design/US-02-merchant-manage-dish.md)。

## Sprint 2

### US-09 Customer registration and unified login
> As a customer, I want to register and log in with one account system, so that I can place orders securely.

Acceptance: registration validates username (unique, case-insensitive), password and phone number; login returns a JWT with `sub`, `role` and `profile_id`; merchants and customers use the same login endpoint. Contract: [../api-contract.md](../api-contract.md).

### SEC-01 RBAC and resource ownership
> As the platform, I want every protected endpoint to check the caller's role and ownership, so that users cannot read or change other users' data.

Acceptance: no token → 401; wrong role → 403; identity is taken from the JWT `profile_id`, never from a client-supplied ID.

### US-14 Merchant and menu browsing
> As a customer, I want to browse merchants and their dishes, so that I can decide what to order.

### INV-FIX Dish soft deactivation
> As a merchant, I want to deactivate a dish without deleting it, so that existing orders and carts keep their references while customers no longer see it.

Status: see [sprint-02-review.md](sprint-02-review.md).

### US-15A Shopping cart
> As a customer, I want to add, change and remove dishes in a cart grouped by merchant, so that I can prepare an order.

Acceptance: prices and totals are calculated by the backend from current dish prices and offers.

### US-15B Delivery address and order creation
> As a customer, I want to choose a delivery address and place an order, so that the merchant can prepare it.

Acceptance: order, order lines and inventory deduction succeed or fail together in one transaction; an empty cart or insufficient stock is rejected.

### US-15C View and cancel pending orders
> As a customer, I want to see my orders and cancel one that is still pending, so that I can change my mind.

Acceptance: only the owner can see or cancel an order; only `Pending` orders can be cancelled; cancelling restores inventory.

## Backlog

See [product-backlog.md](product-backlog.md) for the current, prioritised list (US-10 to US-13, US-16 to US-18).
