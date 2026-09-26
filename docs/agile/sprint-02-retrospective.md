# Sprint 2 — Retrospective

## What went well

- The Sprint 1 foundation extended cleanly into a customer-facing vertical slice: authentication, browsing, cart, address and order processing were developed in parallel and integrated into the shared structure.
- Automated tests grew alongside the features (101 backend, 66 frontend on `main`), including 401/403 authorization tests, and caught integration problems before merge.
- Identity handling was designed correctly from the start: endpoints take the user from the JWT `profile_id` claim rather than from client-supplied IDs.
- CI and CodeQL stayed green on `main` throughout.

## What could have been better

- **API contracts were aligned late.** Frontend and backend pieces were built in parallel before their contracts were fixed, which added integration work for browsing, cart and orders.
- **Integration and CI work started too late.** PostgreSQL integration tests, cross-module tests and security checks were left to the end of the sprint.
- **"Done" was not the same as "on `main`".** INV-FIX, the CI extensions (PostgreSQL, secret scanning) and the address/checkout pages were reported as complete but were still local when the sprint ended. The sprint report and the repository disagreed until they were compared on 26 Sep.
- **Reviews did not happen.** PRs #24, #25 and #26 have no recorded reviewer approval, and `main` branch protection was never confirmed as enabled, so nothing forced a review or a green check before merge.
- Some authentication and RBAC changes reached `main` by direct merge rather than a pull request, so they have no review trail.
- The Sprint 1 review and retrospective documents were never filled in, and story-point splits for US-15A/B/C were not recorded at planning time, which makes the burndown harder to audit.

## What we will try next

| Improvement | Acceptance |
|---|---|
| Agree the API contract and ownership rules in `docs/api-contract.md` before any frontend/backend work on a story starts | The contract PR is merged before the implementation PRs are opened |
| Integrate in small steps: merge to `main` at least every other day and run cross-module CI early | No story is carried in a long-lived branch for more than a few days |
| Define "done" as *merged to `main` with CI green* | Sprint review counts only work verified on `main` |
| Enable branch protection on `main` (PR required, 1 review, `backend` and `frontend` checks required) | Direct pushes and self-merges without a review are rejected |
| Record per-item story points at planning and update the burndown daily | `burndown.md` has a row per day with the point split |
| Fill in the Sprint 1 review/retrospective and keep both documents current each sprint | No "TBD" placeholders left in `docs/agile/` |

## Follow-up on Sprint 1 actions

Sprint 1's retrospective was never completed, so there were no recorded actions to follow up.
