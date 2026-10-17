# Staging runbook

How to run, configure, verify and roll back the staging environment. The design is in [pipeline-design.md](pipeline-design.md).

## Run the whole stack locally

```bash
docker compose --profile app up -d --build
```

- App: <http://localhost:8080> (nginx; `/api` is proxied to the API)
- API: <http://localhost:5080> (health: `/api/health`)
- Demo accounts (created at start-up): `demo_customer`, `demo_merchant`, `sprint3merchant` (second restaurant), `demo_rider`, `admin`; all use `$SEED_PASSWORD` (default `DemoPass123!`, local only)
- Plain `docker compose up -d` still starts PostgreSQL only, for running the API and Vite on your machine.

Smoke test the running stack:

```bash
BASE_URL=http://localhost:8080 SMOKE_PASSWORD='DemoPass123!' node scripts/smoke/staging-smoke.mjs
# API only (no nginx), e.g. `dotnet run`:
BASE_URL=http://localhost:5080 CHECK_FRONTEND=false SMOKE_PASSWORD='...' node scripts/smoke/staging-smoke.mjs
```

Stop and wipe: `docker compose --profile app down -v`.

## Configuration reference

| Setting | Where | Purpose |
|---|---|---|
| `ConnectionStrings__Default` | API env | PostgreSQL connection |
| `Jwt__Key` | API env (secret, ≥ 32 bytes) | JWT signing key |
| `Database__MigrateOnStartup` | API env | apply EF migrations at start-up |
| `Seed__Enabled`, `Seed__Password` | API env | create demo accounts and menu, including the `admin` password (refused in Production) |
| `Admin__InitialPassword` | API env (secret, ≥ 12 chars) | activates the built-in `admin` account once in any environment; the database ships with no usable administrator password |
| `Payment__AllowSimulatedDecline` | API env | enable the decline test card |
| `VITE_ENABLE_TEST_PAYMENT` | frontend **build arg** | show the decline test card in the UI |
| `IMAGE_TAG`, `REGISTRY` | compose / deploy script | which release to run |
| `POSTGRES_PASSWORD`, `JWT_KEY`, `SEED_PASSWORD`, `STAGING_PORT` | `.env` next to `docker-compose.staging.yml` | secrets for the staging compose file |

## GitHub setup for the CD workflow

The default CD run builds, scans and publishes images only. It does not claim a staging deployment. Images use `ghcr.io/<owner>/<repository>/takeout-api` and `takeout-frontend` (lowercase); their OCI source label links them to this repository. Do not reuse another repository's generic `ghcr.io/<owner>/takeout-api` package: its package permissions may deny this repository's `GITHUB_TOKEN`.

For **runner rehearsal**, manually select `ephemeral_rehearsal=true`. It needs no staging secrets and starts a throw-away stack on the Actions runner. Its result is a rehearsal, not persistent staging.

Set repository variable `STAGING_ENABLED=true` only after configuring a persistent staging environment if automatic deployment after each successful main CI is wanted. Leave it unset for the current local-demo setup. A manual `deploy_to_staging=true` requests persistent deployment without enabling automatic deployments. Conflicting manual targets and rollback drills without a target are rejected.

For **remote mode** (a persistent staging server) configure, under *Settings → Secrets and variables → Actions*:

| Name | Kind | Value |
|---|---|---|
| `STAGING_HOST` | secret | server hostname or IP |
| `STAGING_USER` | secret | SSH user that may run `docker` |
| `STAGING_SSH_KEY` | secret | private key of a dedicated deploy key pair |
| `STAGING_SMOKE_PASSWORD` | secret | must equal `SEED_PASSWORD` on the server |
| `STAGING_URL` | variable | public URL, e.g. `http://staging.example.com:8080` |

One-time server preparation:

```bash
# on the server: Docker + compose plugin installed, deploy user in the docker group
mkdir -p ~/takeout-staging && cd ~/takeout-staging
cat > .env <<'ENV'
POSTGRES_PASSWORD=<long random>
JWT_KEY=<at least 32 random bytes>
SEED_PASSWORD=<password for the demo accounts>
STAGING_PORT=8080
ENV
chmod 600 .env
# images are private by default; log in with a token that has read:packages
echo "<PAT>" | docker login ghcr.io -u <github-user> --password-stdin
```

Also create an environment named **staging** (*Settings → Environments*) if you want required reviewers before a deployment runs.

## Deploying

- Automatic image delivery: main CI completes successfully; exact-source CI, CodeQL and Trivy must all pass. Persistent deployment runs only when `STAGING_ENABLED=true`.
- Manual: *Actions → CD → Run workflow*. Select **deploy_to_staging** or **ephemeral_rehearsal** when deployment is intended. Optionally give an existing `image_tag`; select **rollback_drill** together with a deployment target to exercise rollback. With neither target selected and automatic staging disabled, the run only builds/scans/publishes (or validates an existing image).

## Rolling back

Automatic: a failed health check or smoke test redeploys the previous release and re-checks it.

Manual, on the server:

```bash
cd ~/takeout-staging
cat .deployed-tag                      # what is running now
IMAGE_TAG=<previous sha tag> REGISTRY=ghcr.io/<owner>/<repository> bash scripts/deploy/deploy.sh
```

`staging-previous` in the registry always points at the release before the current `staging`.

## Troubleshooting

| Symptom | Check |
|---|---|
| Trivy fails the build | Read the *Scan* table. All HIGH/CRITICAL findings block publication, including unfixed findings. Update the base image/dependency or record the unresolved issue; do not filter it out. |
| API container unhealthy | `docker compose logs api`. Typical causes: `Jwt__Key` shorter than 32 bytes, database not reachable, a migration failing. |
| `Seed:Password must be provided` | `SEED_PASSWORD` is empty. |
| Smoke test fails at login | `STAGING_SMOKE_PASSWORD` differs from `SEED_PASSWORD`, or seeding is disabled. |
| Smoke test fails at "stock reduced exactly once" | Someone else is placing orders on the same staging database while the test runs. |
| `pull access denied` | The server is not logged in to `ghcr.io`, or the token lacks `read:packages`. |

## GHCR publication incident on 17 October 2026

The first CD run after PR #39 failed at `docker push` with `permission_denied: write_package`. Build, image scans and exact-source quality checks had succeeded; deployment was skipped. The existing generic packages `takeout-api` and `takeout-frontend` were linked to `Friedriceee/5001`, not `Friedriceee/DevOps-Jikeda-2026`. The repair uses a repository-specific image prefix and OCI source/revision labels, retaining `GITHUB_TOKEN` and the existing scoped `packages: write` permission. No PAT or changes to the other repository's package access are needed.

A source label does not retroactively grant access to an existing package owned by a different repository. GitHub explains the association and permission rules in [Working with the Container registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry). Publication success still needs a real CD execution; local lint/tests cannot establish registry permission.
