# Sprint 1 — Review

## Review objective

Establish the shared repository and CI foundation, then deliver the merchant
dish and special-offer management slices.

## Delivered scope

| Story / task | Result | Evidence |
|---|---|---|
| US-01/US-02 dish creation and list | Completed | PR #17 and backend/frontend tests |
| US-03/US-04 dish edit and deactivation | Completed | PR #18 and corresponding tests |
| US-05 to US-07 special-offer CRUD | Completed | Merged feature branch and API/UI tests |
| Shared backend/frontend skeleton and CI | Completed | Repository workflow and CI jobs |

## Demonstration

The review flow was: merchant creates a dish, refreshes the list, edits the
dish, deactivates it, and manages a special offer. The API contract and
frontend validation were checked before merge.

## Carry-forward

Authentication, customer ordering and resource ownership were deliberately
carried into Sprint 2. Branch-protection configuration remained a repository
setting to confirm separately from feature completion.
