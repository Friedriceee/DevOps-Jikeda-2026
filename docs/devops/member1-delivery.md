# Member 1 integration and SWE5001 platform delivery

Verified on 17 October 2026. This branch integrates PR #37 (containers/CD) and PR #38 (quality) without changing other members' business logic. The current working application is still the SWE5006 monolith. Four SWE5001 business services have not been extracted. Azure templates and workflows below are prepared and locally validated; no Azure deployment or autoscaling result is claimed.

## What changed

- Preserve PostgreSQL integration, full container smoke and gitleaks from #37, together with the blocking npm gate and evidence from #38.
- Turn .NET JSON vulnerability findings into a blocking High/Critical gate rather than only printing them.
- Require successful CI, CodeQL and Trivy workflows for the exact release SHA. Scan both actual production images before container smoke and before publication, including currently unfixed High/Critical findings.
- Make deployment opt-in while only a local demo exists: default CD publishes scanned images and reports deployment as not requested. `STAGING_ENABLED=true` enables automatic persistent staging; manual `deploy_to_staging` or `ephemeral_rehearsal` selects a target. Requested persistent staging still fails when its credentials are missing.
- Replace the old Debian API / nginx 1.27 runtime bases with supported Alpine variants, pin their runtime digests and explicitly run the frontend as UID 101. Adjust health checks for Alpine and retain backwards-compatible staging checks for the old image.
- Bind local staging to 127.0.0.1 by default. Use a separately configured TLS reverse proxy for shared staging.
- Add an isolated, repeatable old → new → rollback → roll-forward harness. It checks order, stock, coupon and payment fingerprints before/after image switches and runs the 20-step business smoke on every stage. The administrator issues a test coupon before each run through the real API so repeated deployments do not exhaust the initial five seeded coupons; application coupon rules stay intact.
- Add Azure infrastructure, private PostgreSQL networking, four database privileges, per-service identities, Service Bus subscriptions, HTTP/backlog scaling configuration, protected deployment settings and independent-release workflow scaffolding.

## Verified locally

| Check | Result / scope |
|---|---|
| Backend | 287 passed, zero skipped, real PostgreSQL integration enabled |
| Frontend | 126 passed; production build successful |
| npm audit | Zero findings at the time of the run |
| .NET vulnerability gate | Zero High/Critical findings at the time of the run |
| Original runtime images | API 66 / frontend 41 High/Critical records before base updates |
| Updated runtime images | API 0 / frontend 0 High/Critical; unfixed findings are not filtered |
| Clean source snapshot scan | Zero High/Critical vulnerability, secret or misconfiguration findings |
| Release rehearsal | Four stages; each completes 20 smoke steps; database records persist |
| Database role bootstrap | Four roles read/write their own DB, cannot connect to other domain DBs, cannot create tables |
| Bicep and workflows | Bicep compiled with no diagnostics; actionlint passed |

Raw logs, JSON and screenshots are stored under git-ignored `output/member1/`. A passing scan is scoped to its timestamp, image and scanner database; it does not guarantee the absence of future vulnerabilities. The image-switch exercise does not test arbitrary destructive schema rollback.

## Repeat the local rehearsal

Prerequisites: Docker/Compose, Python 3.9+, Node 20.19+ or 22.12+ for the updated frontend. The harness invokes Node for the HTTP smoke. Use the immutable image tags recorded in the delivered evidence, or build a new pair first:

```bash
docker build --pull -t jikeda-member1/takeout-api:sha-<new-commit> backend
docker build --pull --build-arg VITE_ENABLE_TEST_PAYMENT=true \
  -t jikeda-member1/takeout-frontend:sha-<new-commit> frontend
python3 scripts/member1/local-release.py \
  --previous sha-<old-commit> --current sha-<new-commit>
```

The two versions must already exist locally. Defaults: project `jikeda-member1`, URL `http://127.0.0.1:8082`, persistent project-specific PostgreSQL volume. The ignored mode-0600 `.env.member1.local` stores generated local secrets as JSON. The harness reuses this file; do not source it as a shell dotenv file. It never prints credentials. Do not delete the volume or secret file between release stages.

## SWE5001 service handoff contract

See `infra/azure/service-contract.json`. Each of Identity, Catalog, Order and Fulfilment must supply an actual independently deployable image, its own DbContext and migrations, tests inside its build context, `/health/live`, `/health/ready`, and the agreed configuration. Gateway supplies routing and authorization with the same health endpoints. Keep the legacy public API routes compatible where practical so the existing role UI and smoke remain useful.

Run `python3 scripts/member1/service-preflight.py`. It currently exits nonzero and explicitly lists missing contexts. Do not point all five contexts to the monolith to make this check pass. The independent-release workflow additionally refuses an API project without test projects or a test execution containing zero/skipped/failed tests.

Publish messages to `domain-events` with an application property `destination` equal to `order`, `catalog` or `fulfilment`. Subscription filters use that property. For an event needed by multiple services, publish idempotently to each intended destination or revise the topic/filter contract explicitly; a queue with competing consumers is not fan-out. Service-owned Inbox/Outbox and business compensation remain the domain owners' implementation work.

## Azure prerequisites and sequence

No Azure account is signed in on the verified local machine. Before deploying, the environment owner must provide a usable subscription, an existing resource group, region/quota validation, permission to deploy resources and scoped role assignments, and an agreed budget. PostgreSQL, Container Apps and Service Bus may incur charges.

1. Configure an Entra/GitHub OIDC trust bound to this repository's `swe5001-azure` environment. Use a suitable deployment role plus scoped role-assignment permission; applications themselves receive only AcrPull and required Service Bus data roles. Protect the environment as appropriate.
2. Set environment variables `AZURE_RESOURCE_GROUP`, `AZURE_PREFIX`, `AZURE_REGISTRY_NAME`; prefix must be globally unique and 4–20 lowercase letters/digits/hyphens. Set secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, `AZURE_DB_ADMIN_PASSWORD`, `JIKEDA_JWT_KEY`, and four distinct `JIKEDA_<DOMAIN>_DB_PASSWORD` values. The independent smoke also needs protected `STAGING_SMOKE_PASSWORD` and correctly initialized demo accounts.
3. Run the `SWE5001 Azure infrastructure` workflow with `apply=false`. This performs ARM what-if; local Bicep compilation alone does not validate subscription capabilities. Review resource changes and budget. Static Web Apps defaults to East Asia; other resources use the resource group's location. Validate regional availability and quota before apply.
4. Run explicitly with `apply=true` only when ready. Its infrastructure phase does not deploy fake service containers. It creates a private VNet/delegated PostgreSQL subnet and DNS, ACA environment, ACR, four logical databases, per-service identities/roles, messaging, logs and frontend sites.
5. Start `<prefix>-db-bootstrap` once with `az containerapp job start`. Wait for the execution to succeed and check its output. The SQL was locally tested; cloud job execution still needs verification. This job uses protected administrator credentials inside the private network. Runtime roles have DML-only permissions. No database port is exposed publicly.
6. Each domain owner provides a one-shot migration command using a protected migration identity. Execute inside the private ACA network, not by opening PostgreSQL to the public internet. The current contract uses `jikeda_admin` as migration/table owner so default DML grants apply; if adopting separate migration owners, update and test default privileges accordingly. Never give admin credentials to normal application replicas.
7. Build/test/scan/push the five real images to ACR. Create a JSON mapping of all five names to immutable `registry.azurecr.io/service@sha256:...` references. After migrations are verified, run `deploy-azure.py --stage apps --images <mapping> --apply` with protected settings and `JIKEDA_MIGRATIONS_VERIFIED=true`. This script refuses missing service code and mutable images.
8. Deploy the Vue app and independent display to the provisioned Static Web Apps, set the real Gateway base URL and allowed CORS origin, then run four-role smoke and authorization checks. No SWA app publication is claimed by the infrastructure template itself.
9. Use `SWE5001 independent service release` to build/test/scan/deploy one selected service without redeploying the others. It captures the previous image and restores it after a failed update or smoke; readiness and business verification of that restore still require recording before claiming a successful rollback. The workflow is manual until real service boundaries, affected-service dependency rules and shared contracts are stable.
10. Record HTTP 1→2→1 and queue-driven consumer scale-out/scale-in with endpoint metrics, message age/depth and unrelated-service replica counts. Configured scaling rules are not evidence that scale-out has occurred.

## Limits and remaining integration work

- Local code/test/image evidence is complete for the monolith delivery baseline. Remote Actions execution belongs to its actual pushed commit; do not reuse prior PR results as proof for this branch.
- Azure ARM validation, provisioning, initial migrations, frontend publication, cloud release, cloud rollback and scaling are not executed without the actual account and four-service implementation.
- The current `scripts/performance/sprint4-order-readiness.js` is a read/validation load script. Successful write-path 20-user load and post-test distributed invariants must be supplied during the migration.
- This change does not rewrite other members' business functionality, add GPS/real payment/refunds, or claim host/zone high availability from a single local Docker host.
