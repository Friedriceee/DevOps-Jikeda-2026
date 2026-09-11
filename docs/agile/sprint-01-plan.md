# Sprint 1 — 计划

- 周期：第 1 周
- Sprint 目标：打通商家模块两块能力——**菜品管理全流程**（US-01～US-04）与**满减活动管理全流程**（US-05～US-07），CI 绿 + `main` 分支保护开启。

> 范围在 Sprint 中途扩大过一次：最初只排了 US-01，US-01 提前完成后，把同样挂在「商家」聚合下、能复用现有实体/页面结构的 US-02～US-07 一并纳入本 Sprint（见 [product-backlog.md](product-backlog.md) 的分组说明）。商家注册/登录/资料（US-08～US-10）依赖真实身份，放到 Sprint 2。

## 纳入范围

| Story | 估点 | 状态 |
|---|---|---|
| US-01 商家创建菜品 | 5 | Done（PR #17 已合并） |
| US-02 商家查看菜品列表 | 2 | Done（随 US-01 一起实现） |
| US-03 商家编辑菜品 | 3 | In Review |
| US-04 商家下架/删除菜品 | 2 | In Review |
| US-05 商家新增满减活动 | 3 | Todo |
| US-06 商家编辑/删除满减活动 | 2 | Todo |
| US-07 商家查看满减活动列表 | 1 | Todo |
| （技术任务）后端/前端骨架、CI/CD、分支保护 | — | 骨架+CI 已完成，分支保护待确认 |

总估点 18（US-01 原计划外新增 13）。

## 合并顺序

1. 初始架构提交已含 backend/frontend 骨架
2. US-01 走 `feat/us-01-merchant-create-dish` → PR #17（含 US-02 的查询接口）→ **已合并**，CI 配置问题已修
3. US-03/US-04（菜品）、US-05～US-07（满减）各自开 `feat/*` 分支，基于 main 上已有的 `Dish`/`DishController`/`DishService` 继续

## 任务拆分（5+ 人）

| # | 角色 | 范围 | 产出 | 依赖 |
|---|---|---|---|---|
| 1 | 后端骨架 | 技术任务 | ✅ 已完成：EF Migration、`AppDbContext`、DI、Swagger、`/api/health` | — |
| 2 | 后端-菜品 | US-01～US-04 | ✅ `CreateDish`/`GetDishes`（PR #17）；✅ `UpdateDish`/`DeleteDish`（PR 待开） | #1 |
| 3 | 前端骨架 | 技术任务 | ✅ 已完成：路由/布局、`http.js`、Element Plus、`.env` | — |
| 4 | 前端-菜品 | US-01～US-04 | ✅ 创建+列表页（PR #17）；✅ 编辑弹窗 + 下架确认交互（PR 待开） | #3 |
| 5 | 后端-满减 | US-05～US-07 | `SpecialOffer` 实体 + Migration、Create/Edit/Delete/List 接口 | #1 |
| 6 | 前端-满减 | US-05～US-07 | 满减活动页面：新建/编辑/删除/列表，`api/merchant.js` 补方法 | #3 |
| 7 | 契约 + 环境 | 全部 | 维护 `docs/api-contract.md`（补满减接口契约）、review PR #17 | — |
| 8 | CI/CD | 全部 | 修 PR #17 的 CI 配置问题（`cache-dependency-path`、security 作业）、开 `main` 分支保护 | #1 #3 |
| 9 | 后端测试 | 全部 | 菜品 edit/delete 用例、满减 CRUD 用例（成功 + 校验失败 + 越权/不存在） | #2 #5 |
| 10 | 前端测试 | 全部 | 编辑/删除交互测试、满减表单测试 | #4 #6 |
| 11+ | 集成 / QA | 全部 | 端到端手动脚本覆盖 7 个 story、验收清单、缺陷模板，兼 PR review | 全部 |

不足人手：8 并入 2/5，10 并入 4/6。

## Definition of Done

- [x] `docker compose up` 起库，后端连上、Swagger 打开
- [x] `POST /api/merchant/dishes` 创建成功 + 校验分支返回统一结构（PR #17）
- [x] `GET /api/merchant/{id}/dishes` 返回列表（PR #17）
- [x] 菜品可编辑、可下架/删除，前后端均校验（分支 `feat/us-03-04-dish-edit-delete`，待合并）
- [ ] 满减活动可新增/编辑/删除/查看，前后端均校验
- [x] 前端菜品页能填表单创建、列表刷新
- [ ] `main` 分支保护开启（需在 GitHub 网页确认）
- [x] backend / frontend CI job 绿
- [x] PR #17 的 CI 问题修复（`security`、`setup-node` cache 路径）——已在合并前修好
- [ ] 后端 ≥8 用例、前端 ≥6 用例（覆盖菜品 + 满减两块）
- [ ] `docs/api-contract.md` 补齐满减接口，与实现一致

## 技术注意

- 菜品 / 满减活动的 ID 都用数据库自增主键（`ValueGeneratedOnAdd`），不要手写「查全表找最小空位」那类逻辑——并发下会分配到重复 ID。
- 满减活动（`SpecialOffer`）复用 US-01 定的模式：Controller 只编排、校验放 DTO、业务逻辑在 Service、统一 `ApiResult` 返回。
