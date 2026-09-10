# 2. 技术栈选型

- 状态：已接受
- 日期：2026-01

## 背景

旧仓库（`Friedriceee/SoftwareTest`）：ASP.NET Core 6（已 EOL）、Oracle EF Core、Vue 3 + Vuex + Jest（未配置）。作为参考，不继续演进。新仓库从零搭结构。

## 决策

| 方面 | 选择 | 理由 |
|---|---|---|
| 后端框架 | .NET 8（LTS） | 旧的 net6 已停止支持 |
| ORM / 数据库 | EF Core 8 + PostgreSQL | Oracle 在 Apple Silicon 上部署困难；Npgsql 一线支持；容器化简单 |
| 前端 | Vue 3 + Vite + Element Plus | 延续团队已有技术，升级构建链 |
| 前端状态 | Pinia（替代 Vuex） | Vue 3 官方推荐，API 更简单 |
| 前端测试 | Vitest（替代 Jest） | 原生复用 Vite 配置，ESM 无痛 |
| 后端测试 | NUnit + EF Core SQLite in-memory | 不依赖真实数据库，CI 可跑 |
| CI/CD | GitHub Actions | 与仓库集成，免运维 |
| 容器 | Docker Compose（本地 DB） | 环境一致性 |

## 影响

- 生产若强制 Oracle，可切回 `UseOracle`，改动集中在 `Program.cs` 与 provider 包。
- 团队需安装：.NET 8 SDK、Node 20、Docker。
