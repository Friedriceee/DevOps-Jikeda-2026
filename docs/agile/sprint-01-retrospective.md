# Sprint 1 — Retrospective

## What went well

- The team established a shared Vue, ASP.NET Core and PostgreSQL structure.
- The first merchant story was extended into a coherent dish and special-offer
  slice rather than leaving separate CRUD fragments.
- PR review and CI checks exposed workflow configuration issues before merge.
- Service boundaries and a consistent API result shape made later tests easier.

## What could be improved

- The initial plan expanded mid-sprint, so the backlog and estimates needed
  clearer change tracking.
- Some CI and branch-protection setup was treated as a follow-up rather than a
  first-day acceptance condition.
- The team needed a shared contract before adding authentication and customer
  flows.

## Actions for Sprint 2

| Action | Owner | Evidence |
|---|---|---|
| Freeze authentication and customer API contracts before parallel UI work | Team | Sprint 2 plan and API contract |
| Keep acceptance tests with each vertical slice | Team | PR checks and test files |
| Confirm branch protection and required checks in repository settings | Repository owner | GitHub settings evidence |
