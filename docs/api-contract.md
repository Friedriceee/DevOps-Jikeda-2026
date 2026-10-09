# 接口契约

前后端并行开发的唯一依据。改接口先改这个文件，并在 PR 里说明。

## 通用响应格式

所有接口统一返回：

```jsonc
{
  "success": true,
  "data": { /* 业务数据，失败时为 null */ },
  "message": null            // 失败时为错误提示字符串
}
```

HTTP 状态码：成功 `200`（创建 `201`）、参数错误 `400`、未找到 `404`、服务端异常 `500`。

## Iteration 2 认证与授权

### 顾客注册

`POST /api/auth/customer/register`，请求体：`username`（3–50 字符，全局唯一且忽略大小写）、`password`（8–100 字符）、`displayName`（必填，最多 50 字符）、`phoneNumber`（8–11 位数字）。成功返回 `201`，`data` 包含 `accountId`、`customerId`、`username`、`displayName`；重复用户名返回 `409`。

### 统一登录

`POST /api/auth/login`，请求体：`username`、`password`。顾客和商家使用同一接口。成功返回 `200`，`data` 包含 `accessToken`、`expiresAtUtc`、`accountId`、`role`、`profileId`；凭据错误返回 `401`。JWT 包含 `sub`（Account ID）、`role` 和 `profile_id`，密钥从 `Jwt__Key` 环境变量或部署 Secret 注入，长度至少 32 字节。

### RBAC 与资源归属

受保护接口使用 `Authorization: Bearer <token>`。未携带有效 token 返回 `401`，角色无权访问返回 `403`。商家菜品和满减活动的写接口只允许 `Merchant`；顾客地址和订单接口只允许 `Customer`。服务端以 JWT 的 `profile_id` 为身份依据，请求中的 `merchantId` / `userId` 仅为旧客户端兼容字段，不能改变访问主体。商家菜品与满减列表继续允许匿名浏览。

## Sprint 2：顾客购物车

所有购物车接口均要求 `Customer` JWT。顾客身份只取 JWT 的 `profile_id`；请求体不接受 `userId`、`merchantId`、价格或优惠金额。服务端从菜品、商家和 `SpecialOffer` 实时计算金额，按商家分组，并在每组中自动应用符合门槛的最大满减金额。

### 查询购物车

```
GET /api/cart
```

返回的 `data` 包含 `merchants`（每个商家有 `items`、`subtotal`、`discount` 和 `total`）、以及全车 `totalCount`、`subtotal`、`discount` 和 `total`。每个项目包含 `id`（购物车项 ID）、`dishId`、`dishName`、`unitPrice`、`dishNum` 和 `lineTotal`。

### 添加和修改商品

```
POST /api/cart/items
PUT  /api/cart/items/{cartItemId}
```

POST 请求体为 `{ "dishId": 12, "dishNum": 1 }`。相同顾客和菜品会合并为一项并累加数量。PUT 请求体为 `{ "dishNum": 3 }`，将数量设为该绝对值。数量须为正整数且不得超过菜品当前库存；菜品不存在返回 `404`，库存不足返回 `400`。成功均返回最新的完整购物车快照（POST 为 `201`，PUT 为 `200`）。

### 删除商品或清空

```
DELETE /api/cart/items/{cartItemId}
DELETE /api/cart/merchants/{merchantId}
DELETE /api/cart
```

依次用于删除一项、清空一个商家的购物车项和清空整个购物车；成功返回最新的完整购物车快照。购物车项只允许其所属顾客操作，其他顾客访问同一 ID 返回 `404`。

## US-08 商家注册

`POST /api/merchant/register`，请求体：`username`（3–50 字符，唯一）、`password`（6–100 字符）、`merchantName`（必填）、`merchantAddress`（必填）、`contact`（必填）、`dishType`（可选）、`timeForOpenBusiness` 和 `timeForCloseBusiness`（当天秒数，0–86399）、`walletPassword`（6–100 字符）。

成功返回 `201` 和统一响应，`data` 仅含 `id`、`username`、`merchantName`；重复用户名返回 `409`，无效输入返回 `400`。密码及钱包密码仅以哈希值存储，不在响应中返回。钱包与优惠券类型初始化为 0。现有演示商家没有注册账号字段，仍可用于原有菜品和活动接口。

## 第一周：商家创建菜品

### 创建菜品

```
POST /api/merchant/dishes
Content-Type: application/json
```

请求体：

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| merchantId | int | 否 | 旧客户端兼容字段；实际商家身份取自 JWT `profile_id` |
| name | string | 是 | 1–50 字符 |
| price | decimal | 是 | > 0，最多两位小数 |
| category | string | 否 | ≤ 20 字符 |
| imageUrl | string | 否 | 合法 URL，留空用默认图 |
| inventory | int | 是 | ≥ 0 |

响应 `201`：

```json
{ "success": true, "data": { "id": 12, "name": "宫保鸡丁", "price": 28.00, "category": "川菜", "imageUrl": "https://...", "inventory": 100 }, "message": null }
```

响应 `400`（校验失败，示例）：

```json
{ "success": false, "data": null, "message": "price 必须大于 0" }
```

响应 `404`（商家不存在）：

```json
{ "success": false, "data": null, "message": "商家不存在" }
```

### 查询某商家的菜品列表（列表页需要）

```
GET /api/merchant/{merchantId}/dishes
```

响应 `200`：`data` 为菜品数组，元素结构同上。

> 鉴权第一周先跳过，`merchantId` 直接从请求里传。后续再引入登录态。

### 编辑菜品（US-03）

```
PUT /api/merchant/dishes/{dishId}
```

请求体同创建但不含 `merchantId`。商家身份由 JWT `profile_id` 确定，用于校验菜品归属。响应结构同创建；`404` 覆盖「菜品不存在」和「菜品不属于当前商家」两种情况。

### 下架/删除菜品（US-04）

```
DELETE /api/merchant/dishes/{dishId}
```

响应 `200`：`{ "success": true, "data": null, "message": null }`。`404` 同上。当前阶段的“下架”使用删除接口实现，后续接入菜品状态字段后可改为软删除。

## 第一周：商家满减活动

> 实体为 `SpecialOffer`：`merchantId`、`minPrice`（满减门槛）、`amountRemission`（减免金额）。以下接口按此契约落地。

### 新增满减活动（US-05）

```
POST /api/merchant/special-offers
```

请求体：`merchantId`（必填，商家须存在）、`minPrice`（必填，> 0，最多两位小数且不超过 `9999999999999999.99`）、`amountRemission`（必填，> 0，最多两位小数且不超过 `9999999999999999.99`，并且 < `minPrice`）。响应结构同菜品创建。

### 编辑 / 删除满减活动（US-06）

```
PUT    /api/merchant/special-offers/{offerId}
DELETE /api/merchant/special-offers/{offerId}
```

PUT 请求体只包含 `minPrice` 和 `amountRemission`，商家身份由 JWT `profile_id` 确定；规则同新增，且更新不能改变活动所属商家。`404` 覆盖不存在 / 不属于当前商家。

### 查看某商家的满减活动列表（US-07）

```
GET /api/merchant/{merchantId}/special-offers
```

响应 `200`：`data` 为活动数组。
