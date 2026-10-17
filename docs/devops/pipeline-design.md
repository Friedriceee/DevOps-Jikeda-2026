# DevOps / DevSecOps pipeline design

## Branching

- Recommended policy: protect `main`, require one review and all required checks. Repository protection settings must be verified by a repository administrator; they have not been independently confirmed here.
- Short-lived `feat/* fix/* chore/* docs/*` branches, deleted on merge.
- Merge strategy: squash and merge.

## Pipeline overview

```
 pull request ─► CI ─────────────────────────────────────────────┐
                 ├ backend   build · unit/endpoint tests (SQLite) │
                 │           · integration tests (real PostgreSQL)│
                 ├ frontend  Vitest · production build            │  all required
                 ├ security  dependency vulnerability scan        │  before merge
                 ├ secret-scan  gitleaks over the full history    │
                 ├ stack     docker compose build + full-stack    │
                 │           smoke test (checkout → delivery)     │
                 └ codeql    SAST (C#, JavaScript/TypeScript)     ┘
                       │ merged to main, CI green
                       ▼
                 CD  ─ build images ─► Trivy scan ─► push to GHCR (sha tag)
                       ─► deploy to staging ─► health ─► smoke test
                       ─► promote tags (staging, staging-previous)
                       └ on failure: redeploy previous release, verify, mark run failed
```

| Stage | Job / workflow | Tooling | Trigger |
|---|---|---|---|
| Build + unit/endpoint tests | `ci.yml` → `backend`, `frontend` | `dotnet`, NUnit + SQLite in-memory, Vitest, Vite | push, PR |
| Integration tests | `ci.yml` → `backend` (PostgreSQL service) | NUnit against PostgreSQL 16; schema built from the real EF migrations; parallel-transaction tests | push, PR |
| Dependency scan (SCA) | `ci.yml` → `security` | `dotnet list package --vulnerable`, `npm audit` | push, PR |
| Secret scan | `ci.yml` → `secret-scan` | gitleaks (blocking) | push, PR |
| Container smoke | `ci.yml` → `stack` | docker compose, `scripts/smoke/staging-smoke.mjs` | push, PR |
| SAST | `codeql.yml` | GitHub CodeQL | push, PR, weekly |
| Dependency updates | `.github/dependabot.yml` | Dependabot (grouped, no major bumps) | weekly |
| Image build, scan, publish | `cd.yml` → `build` | Docker Buildx, Trivy, GHCR | CI green on `main`, manual |
| Deploy, verify, roll back | `cd.yml` → `deploy` | docker compose, SSH, smoke script | after `build` |

## Continuous deployment

**Artifacts.** Two images, tagged immutably with the commit (`sha-<12 chars>`):

- `takeout-api` — multi-stage .NET 8 build, runs as a non-root user.
- `takeout-frontend` — Vite build served by unprivileged nginx, which also reverse-proxies `/api` to the API so the browser sees one origin.

**Scan before publish.** Images are built into the runner's local Docker daemon and scanned with Trivy (HIGH/CRITICAL, fixable only). A failed scan fails the workflow and **nothing is pushed**.

**Staging target.** One compose file (`docker-compose.staging.yml`) with PostgreSQL, API and frontend, driven by `scripts/deploy/deploy.sh`:

- *Remote mode* — when the `STAGING_HOST`, `STAGING_USER` and `STAGING_SSH_KEY` secrets exist, the release is copied to and started on that server over SSH.
- *Runner mode* — otherwise the same stack is started on the Actions runner, so every release is still deployed, verified and rolled back end to end even without a server. The environment is thrown away afterwards, and all secrets are generated fresh per run.

**Verification.** After the deploy: health checks (`/healthz`, `/api/health`), then the smoke test, which pushes **one order** through the whole lifecycle with real HTTP calls — decline, paid checkout, idempotent replay, merchant accept/prepare/ready, rider accept/pick-up/deliver/complete, customer sees *Completed*.

**Rollback.** Two moving tags are kept in the registry: `staging` (current release) and `staging-previous` (the one before). If any step after the deploy fails, the workflow redeploys the previous release with the same script, checks it is healthy, and still fails the run so the bad release is visible. The *rollback drill* option of the manual run exercises this on demand: it rolls back, smoke-tests the old release, then rolls forward again.

**Database.** The API applies EF migrations at start-up when `Database__MigrateOnStartup=true` and, on non-production environments only, seeds demo accounts when `Seed__Enabled=true`. Both are opt-in per environment.

See [staging-runbook.md](staging-runbook.md) for setup and troubleshooting.

## Minimum security considerations

1. **Secrets stay out of the repository.** `appsettings.Development.json` and `.env` are git-ignored. JWT key, database password, seed password and the administrator password (`Admin__InitialPassword`) are injected from GitHub Secrets or generated per run, and masked in logs. The database itself ships with no usable administrator login. gitleaks fails the build if a credential is committed.
2. **Dependencies.** Dependabot (grouped, no automatic major upgrades) and an SCA scan in every build.
3. **Static analysis.** CodeQL for C# and JavaScript/TypeScript.
4. **Container hardening.** Non-root users, `cap_drop: ALL` and `no-new-privileges` on the API and frontend, no database port published in staging, Trivy gate before publishing.
5. **Least privilege.** Workflows declare `permissions:`; only the jobs that publish get `packages: write`. Remote deploys use a dedicated SSH key.
6. **Input and money handling.** Server-side validation everywhere. Checkout never trusts client prices or identity, is idempotent, and is transactional; see the checkout section of [api-contract.md](../api-contract.md).
7. **Branch protection.** No direct pushes to `main`; reviews and required checks.

## Metrics

- CI pass rate and duration, number of failed deployments, time to roll back, open CodeQL alerts, outdated dependencies.

## Sprint 4 quality integration

Keep the PostgreSQL integration tests, container smoke and secret-scan from PR #37 together with the blocking npm dependency check and quality evidence from PR #38. See [quality gates](../quality/sprint-04-quality-gates.md), [traceability](../quality/traceability-matrix.md) and [performance scope](../quality/performance-test.md). The existing k6 script measures reads and invalid checkout validation; it does not measure successful checkout throughput.
