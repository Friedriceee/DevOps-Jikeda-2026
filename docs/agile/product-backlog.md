# Product Backlog

This is the consolidated backlog for the final report. `Done` means the
implementation exists in the repository and has corresponding tests or demo
evidence. Deferred items remain visible so that the final report does not
overstate the delivered scope.

| ID | User outcome | Priority | Points | Sprint | Status |
|---|---|---:|---:|---:|---|
| US-01 | Merchant creates a dish | High | 5 | 1 | Done |
| US-02 | Merchant views the dish list | High | 2 | 1 | Done |
| US-03 | Merchant edits dish details | High | 3 | 1 | Done |
| US-04 | Merchant deactivates or deletes a dish | Medium | 2 | 1 | Done |
| US-05 | Merchant creates a special offer | Medium | 3 | 1 | Done |
| US-06 | Merchant edits or deletes a special offer | Medium | 2 | 1 | Done |
| US-07 | Merchant views special offers | Medium | 1 | 1 | Done |
| US-08 | Merchant registers an account | High | 3 | 2 | Done |
| US-09 | Customer registers and signs in | High | 5 | 2 | Done |
| SEC-01 | Protected resources enforce JWT role and ownership rules | High | 3 | 2 | Done |
| US-14 | Customer browses merchants and menus | High | 5 | 2/3 | Done |
| US-15 | Customer manages a cart and creates an order | High | 8 | 2/3 | Done |
| US-15-PAY | Customer confirms payment with server-owned pricing | High | 8 | 3/4 | Done |
| US-16 | Merchant fulfils confirmed order states | High | 5 | 3/4 | Done |
| US-17 | Rider accepts and completes delivery | High | 8 | 3/4 | Done |
| US-18 | Administrator manages accounts, coupons and order queries | Medium | 5 | 4 | Done |
| US-19 | Administrator reviews auditable operational events | Medium | 3 | 4 | Done |
| US-20 | Release path validates health, security, performance and rollback | High | 5 | 4 | Evidence status to confirm |
| US-10 | Merchant edits store profile | Medium | 2 | 2 | Deferred |
| US-11 | Merchant manages wallet settlement | Low | 5 | — | Deferred |
| US-13 | Merchant manages delivery area bindings | Low | 3 | — | Deferred |

## Cross-cutting acceptance rules

- Customer, merchant, rider and administrator endpoints derive identity from
  JWT claims and enforce role/ownership checks.
- Server-side pricing is authoritative; payment replay is idempotent.
- Merchant fulfilment must reach `ReadyForDelivery` before rider assignment.
- Inventory restoration and cancellation/rejection effects are exactly once.
- A delivery can be accepted by only one rider and ends at `Completed`.

## Evidence links

- Detailed user-story wording: [user-stories.md](user-stories.md)
- Sprint scope: [sprint-01-plan.md](sprint-01-plan.md),
  [sprint-02-plan.md](sprint-02-plan.md), [sprint-03-plan.md](sprint-03-plan.md),
  [sprint-04-plan.md](sprint-04-plan.md)
- Implementation/test traceability: `../quality/traceability-matrix.md`

The final owner should confirm point totals and deferred stories against the
team's GitHub Project before submission; this file is the report-facing
consolidated view.
