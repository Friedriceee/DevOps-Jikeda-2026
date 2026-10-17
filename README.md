# Takeout Platform

A food delivery platform — **DevOps Group Assignment · 2026 · jikeda**.

Practising DevOps on a full-stack food delivery app: branch protection, CI/CD pipelines, automated testing, containerisation, weekly iterative delivery.

Built from scratch, features delivered iteratively sprint by sprint. Sprint 4
integrates server-priced checkout, payment/idempotency safeguards,
multi-restaurant fulfilment, rider delivery tracking, administrator/audit views,
and the final quality and release evidence package.

## Tech Stack

| | |
|---|---|
| Backend | .NET 8 / ASP.NET Core Web API, EF Core 8, PostgreSQL |
| Frontend | Vue 3, Vite, Element Plus, Vue Router, Pinia |
| Testing | Backend: NUnit + EF Core (SQLite in-memory); Frontend: Vitest + @vue/test-utils |
| CI | GitHub Actions (`.github/workflows/ci.yml`) |

## Prerequisites

- .NET SDK 8.x
- Node.js 20.x
- Docker (to run PostgreSQL locally)

## Getting Started

```bash
# 1. Start the database
docker compose up -d

# 2. Backend
cd backend/src/TakeoutPlatform.Api
cp appsettings.Development.json.example appsettings.Development.json   # first time only
dotnet restore
dotnet tool install --global dotnet-ef        # first time only, installs the EF CLI
dotnet ef database update                      # apply migrations
dotnet run                                      # http://localhost:5080/swagger

# 3. Frontend
cd frontend
npm ci
npm run dev                                     # http://localhost:5173
```

### Or run everything in containers

```bash
docker compose --profile app up -d --build   # PostgreSQL + API + frontend
# app: http://localhost:8080   API: http://localhost:5080
# demo logins: demo_customer / demo_merchant / demo_rider  (password: SEED_PASSWORD, default DemoPass123!)

BASE_URL=http://localhost:8080 SMOKE_PASSWORD='DemoPass123!' node scripts/smoke/staging-smoke.mjs
```

The smoke script walks one order from checkout and simulated payment through merchant fulfilment and rider delivery. The same script gates every deployment; see [docs/devops/staging-runbook.md](docs/devops/staging-runbook.md).

## Project Structure

```
.
├── backend/
│   ├── TakeoutPlatform.sln
│   ├── src/TakeoutPlatform.Api/        # Web API
│   │   ├── Common/                     # ApiResult, global exception filter
│   │   ├── Data/                       # AppDbContext
│   │   └── Features/                   # Organised by feature (Health, Merchant...)
│   └── tests/TakeoutPlatform.Api.Tests/
├── frontend/
│   └── src/{api,router,stores,views,utils}/
├── docs/                              # Deliverables and design docs, see docs/README.md
├── docker-compose.yml                 # local: PostgreSQL, or the whole stack with --profile app
├── docker-compose.staging.yml         # release stack used by CD
├── scripts/                           # smoke test, deploy script
└── .github/workflows/                 # ci.yml, cd.yml, codeql.yml
```

## Documentation

- [docs/README.md](docs/README.md) — Deliverables index (assignment requirements ↔ repo files)
- [docs/agile/sprint-01-plan.md](docs/agile/sprint-01-plan.md) — Sprint 1 plan and task breakdown
- [docs/api-contract.md](docs/api-contract.md) — API contract
- [docs/devops/pipeline-design.md](docs/devops/pipeline-design.md) — CI/CD and DevSecOps pipeline design
- [docs/devops/staging-runbook.md](docs/devops/staging-runbook.md) — run, deploy, verify and roll back staging
- [docs/quality/sprint-04-quality-gates.md](docs/quality/sprint-04-quality-gates.md) — release-blocking quality gates
- [docs/quality/traceability-matrix.md](docs/quality/traceability-matrix.md) — proposal-to-code/test/evidence traceability
- [docs/demo/sprint-04-demo-script.md](docs/demo/sprint-04-demo-script.md) — final demo and screenshot checklist

## Quality commands

```bash
dotnet test backend/TakeoutPlatform.sln -c Release -m:1
npm test --prefix frontend -- --run
npm run build --prefix frontend
BASE_URL=http://localhost:5173 API_URL=http://localhost:5080 \
  SMOKE_PASSWORD="$SMOKE_PASSWORD" node scripts/smoke/staging-smoke.mjs
```

The 20-VU non-mutating k6 gate and its safe checkout-validation probe are
documented in [docs/quality/performance-test.md](docs/quality/performance-test.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Key points: `main` is protected, changes land only via PR, Conventional Commits, squash and merge.
