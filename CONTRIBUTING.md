# 协作与提交规则

## 分支

- `main` 受保护：不能直接 push，只能通过 PR 合入，需 1 个 approve + CI 全绿。
- 从 `main` 切功能分支，命名：
  - `feat/<范围>-<简述>`  新功能，如 `feat/be-create-dish`
  - `fix/<范围>-<简述>`   修 bug
  - `chore/<简述>`        构建/依赖/脚手架
  - `docs/<简述>`         文档
- 一个分支只做一件事，PR 尽量小。

## 提交信息（Conventional Commits）

```
<type>(<scope>): <简述>

<正文，可选，说明为什么>
```

`type`：`feat` `fix` `docs` `chore` `refactor` `test` `ci` `perf`
`scope`（可选）：`backend` `frontend` `ci` `docs` `be` `fe` ……

例：

```
feat(backend): 新增 POST /api/merchant/dishes 创建菜品
test(frontend): 补充菜品表单校验用例
ci: 增加 CodeQL 与依赖漏洞扫描
```

## PR

- 标题同样用 Conventional Commits 风格。
- 按 `.github/pull_request_template.md` 填写。
- 改了接口 → 同步更新 `docs/api-contract.md`。
- 合并方式：**Squash and merge**，squash 后的标题保持规范格式。

## 每个 Sprint 的产物

见 [docs/agile/](docs/agile/)：product backlog、user stories、sprint plan / review / retrospective、burndown。

## Definition of Done

- 代码通过本地 `dotnet test` / `npm run test`
- CI 全绿
- 复杂逻辑有单元测试
- 相关文档已更新
- 至少 1 人 review 通过
