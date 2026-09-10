# Takeout Platform

外卖平台 —— **DevOps 小组作业 · 2026 · jikeda**。

在一个全栈外卖平台上实践 DevOps：分支保护、CI/CD 流水线、自动化测试、容器化、按周迭代交付。

旧代码在 <https://github.com/Friedriceee/SoftwareTest> ，仅作参考，不再改动。本仓库从零搭结构，按周迁移功能，第一周做「商家创建菜品」。

## 技术栈

| | |
|---|---|
| 后端 | .NET 8 / ASP.NET Core Web API、EF Core 8、PostgreSQL |
| 前端 | Vue 3、Vite、Element Plus、Vue Router、Pinia |
| 测试 | 后端 NUnit + EF Core（SQLite in-memory）；前端 Vitest + @vue/test-utils |
| CI | GitHub Actions（`.github/workflows/ci.yml`） |

## 环境要求

- .NET SDK 8.x
- Node.js 20.x
- Docker（起本地 PostgreSQL）

## 本地启动

```bash
# 1. 起数据库
docker compose up -d

# 2. 后端
cd backend/src/TakeoutPlatform.Api
cp appsettings.Development.json.example appsettings.Development.json   # 首次
dotnet restore
dotnet tool install --global dotnet-ef        # 首次，装 EF CLI
dotnet ef database update                      # 建表（有 Migration 后）
dotnet run                                      # http://localhost:5080/swagger

# 3. 前端
cd frontend
npm ci
npm run dev                                     # http://localhost:5173
```

## 目录结构

```
.
├── backend/
│   ├── TakeoutPlatform.sln
│   ├── src/TakeoutPlatform.Api/        # Web API
│   │   ├── Common/                     # ApiResult、全局异常过滤器
│   │   ├── Data/                       # AppDbContext
│   │   └── Features/                   # 按功能分文件夹（Health、Merchant...）
│   └── tests/TakeoutPlatform.Api.Tests/
├── frontend/
│   └── src/{api,router,stores,views,utils}/
├── docs/                              # 交付物与设计文档，见 docs/README.md
├── docker-compose.yml
└── .github/workflows/                 # ci.yml、codeql.yml
```

## 文档

- [docs/README.md](docs/README.md) — 交付物索引（作业交付物 ↔ 仓库文件）
- [docs/agile/sprint-01-plan.md](docs/agile/sprint-01-plan.md) — 第一周计划与任务拆分
- [docs/api-contract.md](docs/api-contract.md) — 接口契约
- [docs/devops/pipeline-design.md](docs/devops/pipeline-design.md) — DevOps/DevSecOps 流水线设计

## 协作规则

见 [CONTRIBUTING.md](CONTRIBUTING.md)。要点：`main` 受保护、只走 PR、Conventional Commits、Squash and merge。
