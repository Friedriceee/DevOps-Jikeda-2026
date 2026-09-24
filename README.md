# Takeout Platform

A food delivery platform — **DevOps Group Assignment · 2026 · jikeda**.

Practising DevOps on a full-stack food delivery app: branch protection, CI/CD pipelines, automated testing, containerisation, weekly iterative delivery.

Built from scratch, features delivered iteratively sprint by sprint. Sprint 1 covers the merchant module: dish management (create/view/edit/remove — done) and special offers (in progress).

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
├── docker-compose.yml
└── .github/workflows/                 # ci.yml, codeql.yml
```

## Documentation

- [docs/README.md](docs/README.md) — Deliverables index (assignment requirements ↔ repo files)
- [docs/agile/sprint-01-plan.md](docs/agile/sprint-01-plan.md) — Sprint 1 plan and task breakdown
- [docs/api-contract.md](docs/api-contract.md) — API contract
- [docs/devops/pipeline-design.md](docs/devops/pipeline-design.md) — DevOps/DevSecOps pipeline design

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Key points: `main` is protected, changes land only via PR, Conventional Commits, squash and merge.
