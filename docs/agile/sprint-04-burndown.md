# Sprint 4 burndown and delivery ledger

This is the evidence-friendly ledger for the final sprint. Replace “Planned”
with the actual date/status during the team review; do not invent completed
points before the corresponding PR or run exists.

| Work item | Owner | Planned evidence | Status |
|---|---|---|---|
| Complete order/payment lifecycle | member1/member2 | backend tests + staging smoke | Planned |
| Merchant/rider fulfilment and tracking | member3/member4 | API/UI tests + demo screenshots | Planned |
| Administrator/audit surfaces | member1/member2 | admin tests + UI evidence | Planned |
| CI/CD and staging deployment | member1 | workflow run + health/deep-link smoke | Planned |
| Traceability, regression, performance, security, final demo | member5 | matrix, test summaries, k6, review/retro | Local evidence complete; staging/CD pending |

## Daily update format

`YYYY-MM-DD — remaining items: N — completed: ... — blocked by: ... — evidence:`
`<PR/CI/staging link>`

The final row is closed only when the release gates and evidence ledger in
`docs/quality/sprint-04-quality-gates.md` are complete.

## Member 5 evidence update

`2026-10-17 — remaining items: staging deployment/CD evidence — completed:
backend 259 passed and 10 skipped, frontend 126 passed, production build,
19-step local PostgreSQL smoke, 20-VU k6 gate, and PR #38 CI/security checks —
evidence: commit 9892f2e; PR #38; local report
Team3_Sprint4_Member3.docx.`
