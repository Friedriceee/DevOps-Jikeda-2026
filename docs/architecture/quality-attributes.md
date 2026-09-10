# Quality Attributes and Strategies

| 质量属性 | 目标 | 策略 | 如何验证 |
|---|---|---|---|
| 可测试性 | 复杂逻辑可单测，不依赖真实数据库 | Service 层依赖注入；测试用 EF Core SQLite in-memory；契约先行 | CI 跑单测；覆盖率报告 |
| 可维护性 | 新人一天内能上手加一个接口 | 按功能分文件夹（Features/）；统一响应/异常；Conventional Commits；小 PR | code review；PR 大小 |
| 安全性 | 最小安全考量 | 密钥不入库（`appsettings.Development.json` 进 gitignore）；依赖漏洞扫描（Dependabot）；SAST（CodeQL）；输入校验（DataAnnotations） | CI 安全作业；CodeQL 告警数 |
| 性能 | 常规请求 P95 < 300ms（本地） | 避免 N+1（EF 显式 Include）；分页；主键用数据库自增而非全表扫描分配 ID | 手动压测 / 后续加基准 |
| 可用性 | CI 绿才能合并，避免坏代码进主干 | 分支保护 + 必需状态检查；健康检查接口 | GitHub branch protection |
| 可移植性 | 一条命令起本地环境 | Docker Compose 起 DB；`.env` 配置化 | README「5 分钟启动」 |

## 权衡记录

- **PostgreSQL 而非 Oracle**：Oracle 在 Apple Silicon 上部署困难，团队环境一致性优先。生产如需 Oracle，EF Provider 可切换。
- **InMemory/SQLite 测试而非 Testcontainers**：第一周求快；后续可引入 Testcontainers 做更真实的集成测试。
