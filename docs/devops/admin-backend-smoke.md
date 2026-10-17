# Administrator Backend — Staging 环境与冒烟清单（member2）

本文件提供管理员后端（Account / Coupon / Order 管理）接入 CD 流水线所需的环境变量、种子账号、健康检查与冒烟步骤，交 member1 汇总到 staging，供 member5 质量门禁使用。

## 模块概览

- 后端目录：`backend/src/TakeoutPlatform.Api/Features/Admin/`
- 路由前缀：`/api/admin`，全部要求 `Authorize(Roles = Administrator)`，身份与 actor 取自 JWT。
- 审计：敏感操作调用 `Features/Audit/IAuditLogger`。member2 默认注册 `NullAuditLogger`；member3 的真实 Audit 实现落地后替换 `Program.cs` 中该行 DI 注册即可。
- 迁移：`20261014081442_AddAccountIsActiveAndSeedAdmin`（新增 `Account.IsActive` 列 + 种子管理员）。

## 环境变量

沿用平台既有变量，无新增：

| 变量 | 说明 | 必需 |
|------|------|------|
| `Jwt__Key` | JWT 签名密钥，≥32 字节。未设置时应用启动即失败。 | 是 |
| `ConnectionStrings__Default` | PostgreSQL 连接串。 | 是 |

## 种子账号（仅 staging / 测试）

| 用户名 | 密码 | 角色 | 说明 |
|--------|------|------|------|
| `admin` | `Seed__Password` 的值（staging）；其它环境为 `Admin__InitialPassword` | Administrator | 内置管理员，用于登录管理后台。仓库里不再提交任何管理员密码；未配置密码时该账号无法登录。 |

> 现有账号在迁移时默认 `IsActive = true`，不影响既有登录。

## 健康检查条件

- 存活：`GET /api/health` 返回 200（平台统一健康端点）。
- 管理模块就绪（带 Administrator token）：`GET /api/admin/accounts` 返回 200 且响应为统一 `ApiResult`（`success=true`）。
- 鉴权正确性：匿名访问 `/api/admin/accounts` 返回 401；Customer/Merchant/Rider token 返回 403。

## 冒烟步骤

前置：以 `admin` 登录拿到 Administrator JWT。

1. 账号查询：`GET /api/admin/accounts?role=Customer&page=1&pageSize=20` → 200，分页结构正确。
2. 停用账号：`PUT /api/admin/accounts/{id}/status` body `{"isActive":false}` → 200；随后该账号登录应失败（`401`）。
3. 重新启用：`PUT /api/admin/accounts/{id}/status` body `{"isActive":true}` → 200；账号恢复可登录。
4. 新建优惠券：`POST /api/admin/coupons`（合法有效期/门槛/金额）→ 201。
5. 优惠券边界：过期区间或金额 ≤ 0 → 400；重复 code → 409。
6. 停用优惠券：`PUT /api/admin/coupons/{id}/status` body `{"isActive":false}` → 200。
7. 订单查询：`GET /api/admin/orders?status=Pending&page=1&pageSize=10` → 200，筛选与分页生效。

### 负向冒烟（鉴权）

- 匿名 → 401；Customer / Merchant / Rider → 403（覆盖所有 `/api/admin/*`）。

## 自动化测试（CI 已覆盖，供流水线证据）

- `AdminServiceTests`：账号查询/启停/角色调整、优惠券创建/重复/无效日期/更新/启停、订单筛选分页。
- `AdminEndpointTests`：401 匿名、403（Customer/Merchant/Rider 参数化）、管理员账号/优惠券/订单 happy path、优惠券金额与日期区间 400。

后端 `dotnet test -c Release`：206 通过 / 5 跳过（并发压力用例）/ 0 失败。

## 待 member3 对接

- `IAuditLogger` / `AuditEvent` 契约已在 `Features/Audit/`。member2 的账号停启、角色调整、优惠券增改启停均已调用 `LogAsync`。member3 落地持久化后，这些操作会自动产生审计记录，无需改 member2 代码。
