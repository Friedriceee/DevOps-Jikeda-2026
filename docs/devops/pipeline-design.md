# DevOps / DevSecOps 流水线设计

## 分支模型

- `main`：受保护，始终可发布。只经 PR 合入，需 1 approve + 必需状态检查通过。
- `feat/* fix/* chore/* docs/*`：短生命周期，合入即删。
- 合并策略：Squash and merge。

## 流水线阶段（`.github/workflows/`）

| 阶段 | 作业 | 工具 | 触发 |
|---|---|---|---|
| 构建 | `backend` / `frontend` | `dotnet build` / `vite build` | push、PR |
| 测试 | 同上 job 内 | NUnit（SQLite in-memory）、Vitest | push、PR |
| 依赖漏洞扫描（SCA） | `security` | `dotnet list package --vulnerable`、`npm audit --audit-level=high` | push、PR（High/Critical 阻断） |
| SAST | `codeql` | GitHub CodeQL（C# + JS） | push、PR、每周定时 |
| 文件系统 / 发布镜像扫描 | `trivy` | Trivy（CRITICAL/HIGH；镜像由 `vars.TRIVY_IMAGE` 提供） | push、PR、每周定时 |
| 依赖更新 | Dependabot | `.github/dependabot.yml` | 每周 |
| 部署（后续） | `deploy` | 前端 → GitHub Pages；后端待定 | `main` push |

## 工具选型理由

- **GitHub Actions**：与仓库零集成成本，配置即代码。
- **CodeQL**：GitHub 原生 SAST，公开仓库免费，支持 C#/JS。
- **Dependabot**：原生依赖更新 + 安全告警。
- **Docker Compose**：本地环境一致性（PostgreSQL）。

## 最小安全考量

1. **密钥不入库**：`appsettings.Development.json` 在 `.gitignore`；只提交 `.example`。CI 用环境变量/GitHub Secrets。
2. **依赖漏洞**：Dependabot 告警 + CI 里 `npm audit --audit-level=high` / `dotnet list package --vulnerable`；未处理的 High/Critical 阻断合并。
3. **静态分析**：CodeQL 扫注入、硬编码凭据等。
4. **最小权限**：workflow `permissions:` 显式收窄；PAT 用 fine-grained / 最小 scope。
5. **输入校验**：DTO 层 DataAnnotations，服务端不信任前端。
6. **分支保护**：禁止直推 `main`，强制 review。

## 度量

- CI 通过率、平均修复时间（MTTR）、CodeQL 未处理告警数、依赖过期数。

## Sprint 4 release gate

最终交付的质量门禁、traceability matrix、20-VU 性能脚本、全链路 HTTP
smoke test、演示步骤和截图清单集中维护在
[quality/sprint-04-quality-gates.md](../quality/sprint-04-quality-gates.md)。
性能门禁要求 20 个并发虚拟用户下 p95 < 2 秒；成功 checkout 仍由 smoke
test 验证，k6 默认只发送不会改变订单/库存的 checkout 校验请求。
