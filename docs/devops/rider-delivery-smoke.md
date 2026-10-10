# Rider Delivery — Staging 冒烟与 CD 交付清单（member5）

本文件提供骑手配送模块（Rider Chain / Delivery）接入 CD 流水线所需的环境变量、健康检查条件与冒烟步骤，交 member1 汇总到 staging 部署。

## 模块概览

- 后端特性目录：`backend/src/TakeoutPlatform.Api/Features/Rider/`
- 路由前缀：`/api/rider`，全部要求 `Authorize(Roles = Rider)`，身份取自 JWT `profile_id`（不信任请求参数）。
- 数据表：`Riders`、`RiderDeliveries`（迁移 `202610110001_AddRiderDelivery`）。
- 上游依赖：消费 member4 产出的 `Order.Status == ReadyForDelivery` 订单。

## 环境变量

骑手模块本身不新增独立配置，沿用平台既有变量：

| 变量 | 说明 | 必需 |
|------|------|------|
| `Jwt__Key` | JWT 签名密钥，≥32 字节。未设置时应用启动即失败。 | 是 |
| `ConnectionStrings__Default` | PostgreSQL 连接串（骑手表随统一迁移创建）。 | 是 |

## 数据库迁移

骑手两张表包含在统一的 EF 迁移中，staging 部署时执行迁移即可建表并写入种子骑手：

- 种子骑手（供演示/冒烟）：
  - `Id=1` Rider Zhang — ZoneA，在职，可用，容量 3
  - `Id=2` Rider Li — ZoneA，在职，可用，容量 2
  - `Id=3` Rider Off — ZoneA，**离职**（不可派单，用于演示责任链拒绝）

> 骑手账号登录：骑手数据由平台预置，`Account.Role=Rider`、`Account.ProfileId` 指向上述 `Rider.Id`。

## 健康检查条件

- 存活：`GET /api/health` 返回 200（平台统一健康端点）。
- 骑手模块就绪（可选，带 Rider token）：`GET /api/rider/available` 返回 200 且响应体为统一 `ApiResult` 结构（`success=true`）。
- 鉴权正确性：匿名访问 `GET /api/rider/available` 返回 401；非骑手角色返回 403。

## 冒烟步骤（骑手段，接在整条订单链路之后）

前置：顾客已下单并支付、商家已把该订单推进到 `ReadyForDelivery`（member1/2/3/4 的链路）。记该订单号为 `ORDER_ID`。

1. 以骑手账号登录，拿到 Rider 角色 JWT。
2. 查可接任务：`GET /api/rider/available` → 列表应包含 `ORDER_ID`。
3. 接单：`POST /api/rider/deliveries/{ORDER_ID}/accept` → 200，`data.status == "Assigned"`；此时订单状态变为 `Assigned`。
4. 取餐：`PUT /api/rider/deliveries/{ORDER_ID}/status` body `{"action":"PickUp"}` → 200，`status == "PickedUp"`。
5. 开始配送：`PUT .../status` body `{"action":"StartDelivering"}` → 200，`status == "Delivering"`。
6. 送达完成：`PUT .../status` body `{"action":"Complete"}` → 200，`status == "Completed"`；订单状态变为 `Completed`。
7. 校验：`GET /api/rider/deliveries` 显示该单为 `Completed`；可接任务池不再包含 `ORDER_ID`。

### 负向冒烟（验证鲁棒性）

- 并发/重复接单：对同一 `ORDER_ID` 第二次 `accept` → 409（AlreadyTaken）。
- 非法状态推进：刚接单（Assigned）直接 `Complete` → 409（InvalidTransition）。
- 越权：以非骑手 token 调用任意 `/api/rider/*` → 403；匿名 → 401。

## 自动化测试（CI 已覆盖，供流水线证据）

- `RiderAssignmentChainTests`（责任链：无人可派 / 错区域 / 满容量 / 选择）。
- `RiderDeliveryServiceTests`（接单准入、并发只一个成功、全生命周期流转、库存计数、可接池过滤）。
- `RiderEndpointTests`（401 / 403 / 接单 happy path / 404）。
- 前端 `utils/__tests__/delivery.spec.js`、`api/__tests__/rider.spec.js`。

后端 `dotnet test -c Release` 与前端 `npm run test` 均全绿。
