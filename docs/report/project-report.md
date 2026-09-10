# Project Report

> 老师会提供正式模板。拿到后把内容迁到模板里，这里先按提纲组织素材。

## 1. Introduction

- 项目背景与目标
- 范围（本学期实现哪些 user story）
- 团队与分工

## 2. Design and Architecture

- 系统上下文图、容器图（C4 Level 1–2）
- 分层：前端 SPA / 后端 Web API / 关系型数据库
- 关键架构决策 → 见 [../architecture/decisions/](../architecture/decisions/)
- 分析到设计的转化策略 → 见 [../architecture/high-level-design.md](../architecture/high-level-design.md)

## 3. Quality Attributes and Strategies

见 [../architecture/quality-attributes.md](../architecture/quality-attributes.md)。至少覆盖：可测试性、可维护性、安全性、性能、可用性。

## 4. Design Patterns

见 [../architecture/design-patterns.md](../architecture/design-patterns.md)。

## 5. DevOps and Development Lifecycle

见 [../devops/pipeline-design.md](../devops/pipeline-design.md) 与 [../agile/](../agile/)。

- 分支模型与 PR 流程
- CI/CD 流水线各阶段
- DevSecOps：依赖扫描、SAST、密钥管理
- 迭代节奏与度量（burndown、CI 通过率、评审）

## 6. Testing

- 测试策略（单元 / 集成 / 端到端）
- 覆盖率现状
- 复杂类的测试清单

## 7. Retrospective

每个 sprint 的回顾汇总，见 [../agile/](../agile/)。
