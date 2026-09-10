# Design Patterns

记录项目中「用设计模式解决的设计问题」。每条：问题 → 模式 → 落地位置。

## 已应用

| 问题 | 模式 | 落地 |
|---|---|---|
| Controller 直接写数据访问，难测、职责混乱 | **分层 + Repository/Service** | 业务逻辑收敛到 `Service`，Controller 只做编排 |
| 每个接口手写成功/失败 JSON，格式不一 | **统一响应包装（DTO / Result Object）** | `Common/ApiResult<T>` |
| 未捕获异常把堆栈抛给前端 | **拦截器 / 责任链** | `Common/ApiExceptionFilter`（`IExceptionFilter`） |
| 前端每个请求都写重复的解包和错误提示 | **拦截器（Interceptor）** | `frontend/src/api/http.js` axios interceptor |
| 依赖散落、难替换（如换数据库、mock） | **依赖注入（DI）** | ASP.NET Core 内置容器，`Program.cs` 注册 |

## 候选（随功能引入时补充）

- **工厂 / 策略**：不同角色的下单/计费逻辑
- **观察者 / 事件**：订单状态变更通知骑手
- **规约（Specification）**：复杂查询条件组合

> 原则：ID 分配交给数据库自增主键，不手写「读全表找空位」的算法——并发下会分配重复 ID。属于「用平台机制替代手写算法」。
