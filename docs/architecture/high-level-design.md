# High-Level Design

## 1. 系统上下文

外卖平台，四类角色：顾客、商家、骑手、平台管理员。本学期范围以商家侧为主，第一个 user story 是「商家创建菜品」。

```
[顾客/商家/骑手/管理员] --HTTP--> [Vue SPA] --REST/JSON--> [ASP.NET Core API] --EF Core--> [PostgreSQL]
```

## 2. 分层

| 层 | 技术 | 职责 |
|---|---|---|
| 表现层 | Vue 3 + Element Plus | 页面、表单校验、状态管理（Pinia） |
| API 层 | ASP.NET Core Controller | 路由、模型校验、统一响应包装 |
| 业务层 | Service | 领域逻辑、事务边界 |
| 数据层 | EF Core + PostgreSQL | 持久化、迁移 |

横切：全局异常过滤器、日志、CORS、（后续）鉴权。

## 3. 从分析到设计的转化策略

1. **用例 → user story**：把需求拆成可独立交付的 story，写验收标准（见 [../analysis-design/](../analysis-design/)）。
2. **领域名词 → 实体**：Merchant、Dish、Order… 名词建模为 EF 实体；动词建模为 Service 方法。
3. **接口先行**：每个 story 先在 [../api-contract.md](../api-contract.md) 定契约，前后端据此并行。
4. **一条竖切一次交付**：每个 story 打通「页面 → API → DB → 测试」，不做纯水平分层的半成品。
5. **持续重构**：发现设计问题（响应格式不统一、并发下的 ID 分配 bug 等）及时在新结构里修正，不积累技术债。

## 4. 关键组件

- `AppDbContext` — 唯一的 EF 上下文
- `ApiResult<T>` — 统一响应结构
- `ApiExceptionFilter` — 兜底异常 → 500 + 统一结构
- `frontend/src/api/http.js` — axios 实例，拦截器拆响应外壳

## 5. 数据模型（第一周）

```
Merchant (Id PK, Name, ...)
Dish (Id PK, MerchantId FK, Name, Price, Category, ImageUrl, Inventory)
```

主键用数据库自增（`ValueGeneratedOnAdd`），不手写分配。

## 6. 待定 / 后续

- 鉴权与会话（JWT）
- 订单、骑手调度等其余模块
- 文件/图片存储方案
