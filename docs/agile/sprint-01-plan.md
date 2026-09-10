# Sprint 1 — 计划

- 周期：第 1 周
- Sprint 目标：交付 **US-01 商家创建菜品**，端到端跑通（前端填表单 → 后端落库 → 列表刷新），CI 绿 + `main` 分支保护开启。

## 纳入范围

| Story | 估点 |
|---|---|
| US-01 商家创建菜品 | 5 |
| （技术任务）后端 / 前端骨架、CI/CD、分支保护 | — |

## 合并顺序

1. 初始架构提交已含 backend/frontend 骨架，直接在其上开分支
2. 各自 `feat/*` 分支 → PR → review → 合入 `main`

## 任务拆分（5+ 人）

| # | 角色 | 产出 | 依赖 |
|---|---|---|---|
| 1 | 后端骨架 | 首个 EF Migration（Merchant、Dish）、`AppDbContext` 补 DbSet、DI 接 PostgreSQL、Swagger、`/api/health` | — |
| 2 | 后端功能 | `POST /api/merchant/dishes` + `GET /api/merchant/{id}/dishes`：`CreateDishRequest` DTO + 校验、`DishService.Create`、商家存在性校验、统一 `ApiResult` | #1 |
| 3 | 前端骨架 | 路由/布局、`api/http.js` 拦截器联调、Element Plus 主题、`.env` 配 `VITE_API_BASE` | — |
| 4 | 前端功能 | 菜品页：列表 + 新建表单（el-form 校验）、`createDish`/`listDishes`、成功刷新 + toast | #3 |
| 5 | 契约 + 环境 | 维护 `docs/api-contract.md`、`docker-compose.yml`、README、Swagger 联调 | — |
| 6 | CI/CD | 调绿流水线、配 `main` 分支保护（必需检查：backend、frontend）、PR 模板 | #1 #3 |
| 7 | 后端测试 | `CreateDish`：成功 / 缺 name→400 / price≤0→400 / inventory<0→400 / 商家不存在→404 | #2 |
| 8 | 前端测试 | 组件测试：空名字被拦、提交 payload 正确、成功弹 toast | #4 |
| 9+ | 集成 / QA | 端到端手动脚本、验收清单、缺陷模板，兼 PR review | #2 #4 |

不足 5 人：6/7 并入 2、8 并入 4。

## Definition of Done

- [ ] `docker compose up` 起库，后端连上、Swagger 打开
- [ ] `POST /api/merchant/dishes` 创建成功 + 4 个校验分支返回统一结构
- [ ] `GET /api/merchant/{id}/dishes` 返回列表
- [ ] 前端菜品页能填表单创建、列表刷新
- [ ] CI 两条 job 绿；`main` 保护开启
- [ ] 后端 ≥4 用例、前端 ≥3 用例
- [ ] `docs/api-contract.md` 与实现一致

## 技术注意

旧代码 `AssignDishId` 把全表 ID 读进内存找空位，并发下分配重复 ID。改用数据库自增主键（`ValueGeneratedOnAdd`），不手写。
