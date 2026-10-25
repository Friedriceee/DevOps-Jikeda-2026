# 文档 / 交付物索引

作业交付物与本仓库文件的对应关系。每项由括号里的负责人维护。

## Deliverables — 1

| 交付物 | 位置 |
|---|---|
| Working system | `backend/` + `frontend/`，CI 部署产物 |
| Code base | 本仓库 <https://github.com/Friedriceee/DevOps-Jikeda-2026> |
| Project Report（用提供的模板） | [report/project-report.md](report/project-report.md) |
| — Design & Architecture + 关键架构决策 | [architecture/high-level-design.md](architecture/high-level-design.md)、[architecture/decisions/](architecture/decisions/) |
| — Quality attributes & strategies | [architecture/quality-attributes.md](architecture/quality-attributes.md) |
| — DevOps & development lifecycle | [devops/pipeline-design.md](devops/pipeline-design.md) |
| DevSecOps pipeline scripts & test scripts | `.github/workflows/`、`backend/tests/`、`frontend/src/**/__tests__/` |

## Deliverables — 2

| 交付物 | 位置 |
|---|---|
| Presentation slides | [slides/](slides/) |
| Agile 过程产物（backlog / stories / sprint plan / burndown / review / retro） | [agile/README.md](agile/README.md) |
| High-level design（含 分析→设计 的转化策略） | [architecture/high-level-design.md](architecture/high-level-design.md) |
| 在范围内的 user story 详细分析与设计 | [analysis-design/](analysis-design/) |
| 用设计模式解决的设计问题 | [architecture/design-patterns.md](architecture/design-patterns.md) |
| 结构良好的源码 + 复杂类的单元测试 | `backend/`、`frontend/`、对应 tests |
| DevOps 流水线设计 + 工具 + 最小安全考量 | [devops/pipeline-design.md](devops/pipeline-design.md) |

## 其它

- [api-contract.md](api-contract.md) — 前后端接口契约
- [../CONTRIBUTING.md](../CONTRIBUTING.md) — 分支与提交规则

## Sprint 4 quality and delivery

| 交付物 | 位置 |
|---|---|
| Proposal → code/API/tests/evidence traceability | [quality/traceability-matrix.md](quality/traceability-matrix.md) |
| Release-blocking quality gates | [quality/sprint-04-quality-gates.md](quality/sprint-04-quality-gates.md) |
| 20-VU performance runbook | [quality/performance-test.md](quality/performance-test.md) and `../scripts/performance/sprint4-order-readiness.js` |
| Final demo and screenshot script | [demo/sprint-04-demo-script.md](demo/sprint-04-demo-script.md) |
| Sprint 4 review / retrospective / burndown | [agile/sprint-04-review.md](agile/sprint-04-review.md), [agile/sprint-04-retrospective.md](agile/sprint-04-retrospective.md), [agile/sprint-04-burndown.md](agile/sprint-04-burndown.md) |
