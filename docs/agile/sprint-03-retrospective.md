# Sprint 3 — Retrospective

## What went well

- The team made pricing, fulfilment and rider assignment explicit service
  boundaries instead of putting the rules in controllers or browser code.
- Server-owned totals, idempotency and inventory compensation reduced the risk
  of duplicate orders and inconsistent stock.
- State transitions and rider eligibility became visible in focused tests and
  in the end-to-end demo path.
- The container smoke job gave the team a shared integration check beyond
  isolated unit tests.

## What could be improved

- Shared contracts and integration work still competed with feature work near
  the end of the sprint.
- Staging environment, registry permissions and rollback evidence needed to be
  exercised earlier than the final documentation phase.
- Some final report figures were captured from local branches and therefore
  needed clearer environment and commit labels.

## Actions for Sprint 4

| Action | Owner | Evidence |
|---|---|---|
| Start from a clean, synchronized main branch | Team | Git status and commit SHA |
| Treat health, smoke, security and performance checks as release gates | member5 + team | Quality gates and CI runs |
| Use one order identifier and consistent state vocabulary in the demo | Team | Demo script and screenshots |
| Label local, rehearsal and persistent staging evidence separately | member5 | Report evidence index |
