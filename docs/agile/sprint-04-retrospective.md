# Sprint 4 retrospective

Complete this record after the review. Keep observations evidence-based and
link to a commit, issue, CI run, or screenshot where possible.

## Start / stop / continue

| Start | Stop | Continue |
|---|---|---|
| Run the HTTP smoke test and performance gate before the demo rehearsal. | Treating a local green unit suite as proof that staging wiring works. | Small PRs with explicit API, test, and evidence links. |
| Record multi-merchant test data and account ownership in the demo plan. | Sharing passwords or JWTs in screenshots and reports. | Server-side pricing, idempotency, and rollback checks. |

## What we learned

- A multi-restaurant order is not rider-ready until every merchant fulfilment is
  `ReadyForDelivery`.
- SQLite tests are fast and deterministic, while PostgreSQL concurrency and
  deployment checks must be run separately and labelled honestly.
- Read-only performance pressure should be separated from the mutating happy
  path, which is better proven by the staging smoke test.

## Actions for the next iteration

1. Attach CI/staging URLs and real performance numbers to the traceability
   matrix.
2. Keep release-blocking security thresholds visible in branch protection.
3. Retain the demo seed data and document how it can be reset safely.
