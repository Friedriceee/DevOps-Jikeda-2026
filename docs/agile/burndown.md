# Burndown

## 跟踪方式

- 用 GitHub Projects（看板 + Insights 自带 burndown），或手工维护下表。
- 每天更新「剩余估点」。

## Sprint 1

总估点：5（US-01）+ 骨架技术任务

| 日期 | 剩余估点 | 备注 |
|---|---|---|
| Day 1 | 5 | Sprint 开始 |
| Day 2 | | |
| Day 3 | | |
| Day 4 | | |
| Day 5 | | Sprint 结束 |

理想线：从 5 均匀降到 0。实际线：按上表。

> 导出图表放进报告和 slides。

## Sprint 2

Period: 15 Sep – 26 Sep 2026. Planned: 24 story points. Ideal line: linear from 24 to 0 across the 11 days after Day 1.

Actual values are read from the burndown chart in the Sprint 2 progress report (Figure 13); they are integers taken off the plotted points, so treat them as ±0.5.

| Date | Ideal remaining | Actual remaining | Note |
|---|---|---|---|
| 15 Sep | 24.0 | 24 | Sprint start |
| 16 Sep | 21.8 | 24 | Authentication design, shared API contract |
| 17 Sep | 19.6 | 22 | |
| 18 Sep | 17.5 | 20 | |
| 19 Sep | 15.3 | 18 | |
| 20 Sep | 13.1 | 16 | |
| 21 Sep | 10.9 | 14 | |
| 22 Sep | 8.7 | 11 | |
| 23 Sep | 6.5 | 8 | |
| 24 Sep | 4.4 | 6 | Auth/RBAC (`9768df8`), customer frontend (PR #25) and address/orders (PR #24) merged |
| 25 Sep | 2.2 | 3 | |
| 26 Sep | 0.0 | 0 | Cart (PR #26) merged. INV-FIX (3 pts) is counted as done in the chart but is not on `main` — see [sprint-02-review.md](sprint-02-review.md) |

> The point split for US-15A/B/C was not recorded, so the daily drop cannot be attributed to individual stories.

