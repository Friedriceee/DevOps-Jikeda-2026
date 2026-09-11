# US-02 商家查看、编辑和下架菜品

## User Story

> 作为商家，我想查看、编辑和下架我的菜品，以便及时维护菜单内容。

## 验收标准

- [x] 商家可以查看自己的菜品列表。
- [x] 商家可以编辑菜品名称、价格、分类、图片和库存。
- [x] 编辑时前端和服务端都会执行字段校验。
- [x] 商家不能编辑或下架其他商家的菜品，接口返回 `404`。
- [x] 商家可以下架菜品，下架后菜品不再出现在列表中。
- [x] 编辑和下架接口有自动化测试，前端提供对应操作入口。

## 分析与设计

- 参与者：商家。第一阶段暂不接入登录，`merchantId` 由前端传入。
- 查看流程：进入菜品页 → `GET /api/merchant/{merchantId}/dishes` → 展示列表。
- 编辑流程：点击 Edit → 回填表单 → 前端校验 → `PUT /api/merchant/dishes/{dishId}?merchantId={merchantId}` → 服务端校验归属并保存 → 刷新列表。
- 下架流程：点击 Delist → 二次确认 → `DELETE /api/merchant/dishes/{dishId}?merchantId={merchantId}` → 删除成功 → 刷新列表。
- 异常流：字段非法返回 `400`；菜品不存在或不属于当前商家返回 `404`；未处理异常返回统一 `500`。

## 代码范围

```text
backend/src/TakeoutPlatform.Api/Features/Merchant/
  DishController.cs       PUT / DELETE endpoints
  DishService.cs          ownership check, update and delete
  UpdateDishRequest.cs    update DTO and validation

frontend/src/api/merchant.js
  updateDish(), deleteDish()
frontend/src/views/merchant/MerchantDishView.vue
  edit form, save/cancel, delist confirmation and list actions
```

## 测试范围

- 后端：编辑成功、编辑他人菜品返回 `404`、编辑参数非法返回 `400`、下架成功、下架他人菜品返回 `404`。
- 前端：编辑请求 payload 不包含 `merchantId`，可选字段会被正确清理。
