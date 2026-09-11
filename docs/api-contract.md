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

## 第一周：商家创建菜品

### 创建菜品

```
POST /api/merchant/dishes
Content-Type: application/json
```

请求体：

| 字段 | 类型 | 必填 | 规则 |
|---|---|---|---|
| merchantId | int | 是 | 商家必须存在 |
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

请求体同创建（含 `merchantId`，用于校验菜品归属）。响应结构同创建；`404` 覆盖「菜品不存在」和「菜品不属于该商家」两种情况，不区分返回，避免探测其他商家的菜品 ID。

### 下架/删除菜品（US-04）

```
DELETE /api/merchant/dishes/{dishId}?merchantId={merchantId}
```

响应 `200`：`{ "success": true, "data": null, "message": null }`。`404` 同上。

## 第一周：商家满减活动

> 实体为 `SpecialOffer`：`merchantId`、`minPrice`（满减门槛）、`amountRemission`（减免金额）。以下待 US-05～US-07 实现时按此契约落地，规则允许在开发中微调，改动需同步这里。

### 新增满减活动（US-05）

```
POST /api/merchant/special-offers
```

请求体：`merchantId`（必填，商家须存在）、`minPrice`（必填，≥ 0）、`amountRemission`（必填，> 0 且 < `minPrice`）。响应结构同菜品创建。

### 编辑 / 删除满减活动（US-06）

```
PUT    /api/merchant/special-offers/{offerId}
DELETE /api/merchant/special-offers/{offerId}?merchantId={merchantId}
```

规则同新增；`404` 覆盖不存在 / 不属于该商家。

### 查看某商家的满减活动列表（US-07）

```
GET /api/merchant/{merchantId}/special-offers
```

响应 `200`：`data` 为活动数组。
