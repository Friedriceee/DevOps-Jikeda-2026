# US-01 商家创建菜品

## User Story

> 作为**商家**，我想**新增一道菜品**，以便**顾客能在我的店里看到并下单**。

## 验收标准

- [ ] 商家在菜品页填写：名称、价格、（可选）分类、（可选）图片、库存
- [ ] 名称必填、1–50 字符；价格必须 > 0；库存 ≥ 0；违反时表单拦截并提示
- [ ] 提交成功后，新菜品出现在列表里，且给出成功提示
- [ ] 商家不存在时接口返回 404 与统一错误结构
- [ ] 服务端对所有字段再校验一次（不信任前端）

## 分析

- 参与者：商家（本周不做登录，`merchantId` 由前端传）
- 主流程：填表 → 前端校验 → `POST /api/merchant/dishes` → 服务端校验 + 落库 → 返回新 `id` → 前端刷新列表
- 异常流：字段非法 → 400；商家不存在 → 404；数据库异常 → 500（统一结构）

## 设计

### 接口

见 [../api-contract.md](../api-contract.md)。

### 后端

```
Features/Merchant/
  Dish.cs                 实体（Id 自增主键）
  Merchant.cs             实体
  CreateDishRequest.cs    DTO + DataAnnotations 校验
  DishController.cs       POST /api/merchant/dishes, GET /api/merchant/{id}/dishes
  DishService.cs          Create(): 校验商家存在 → 建实体 → SaveChanges
```

- `AppDbContext` 增加 `DbSet<Merchant>`、`DbSet<Dish>`
- 迁移：`dotnet ef migrations add CreateMerchantAndDish`

### 前端

```
src/api/merchant.js       createDish(payload), listDishes(merchantId)
src/views/merchant/MerchantDishView.vue   列表 + 新建表单（el-form 规则）
src/stores/merchant.js    当前 merchantId
```

### 测试

| 层 | 用例 |
|---|---|
| 后端 | 成功创建 / 缺名称→400 / 价格≤0→400 / 库存<0→400 / 商家不存在→404 |
| 前端 | 空名称被拦 / 提交 payload 正确 / 成功弹 toast 并刷新 |

## 涉及的设计模式

统一响应包装、DI、（前端）拦截器。见 [../architecture/design-patterns.md](../architecture/design-patterns.md)。
