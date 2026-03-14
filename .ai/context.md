# ScoreCast — Project Context

## Overview
Premier League predictions app where users predict match outcomes and scorelines. Points awarded based on accuracy with leaderboards across prediction leagues. Built with .NET 10 and clean architecture.

## Tech Stack
- .NET 10 (SDK 10.0.101), C# latest
- FastEndpoints 8.0.1 (REPR pattern + CQRS, no MediatR)
- Entity Framework Core 10.0.4 with Npgsql (Neon cloud PostgreSQL)
- Blazor WASM (standalone) with MudBlazor 9.1.0
- Keycloak 26.1 — self-hosted OIDC/OAuth2 (Docker)
- Refit 10.0.1 — typed HTTP API clients
- Serilog — structured logging
- Central Package Management, `.slnx` solution format

## Architecture
- **CQRS without MediatR** — commands/queries in Application, handlers in Infrastructure via FastEndpoints
- **Queries** use `IQuery<T>` / `IQueryHandler<TQuery, TResult>` interfaces (extend FastEndpoints `ICommand<T>` / `ICommandHandler`)
- **Commands** use `ICommand<T>` / `ICommandHandler<TCommand, TResult>` from FastEndpoints directly
- **Clean Architecture** — Domain has zero dependencies (except Shared), Application depends on Domain, Infrastructure implements Application interfaces
- **Separate Migrations project** — excluded from default build to avoid slow compile times

## Project Structure
```
Solution
├── .ai/                               → AI agent context, coding standards, review checklist
├── Migrations/
│   └── ScoreCast.Ws.Migrations        → EF migrations (excluded from default build)
├── keycloak/
│   └── themes/scorecast/login/        → Custom Keycloak login/register theme
├── src/APIs/
│   ├── ScoreCast.Ws                   → API host, DI, middleware (port 5105)
│   ├── ScoreCast.Ws.Application       → Commands, queries, interfaces
│   ├── ScoreCast.Ws.Domain            → Entities, value objects
│   ├── ScoreCast.Ws.Endpoints         → FastEndpoints definitions, preprocessors
│   ├── ScoreCast.Ws.Infrastructure    → Handlers, DbContext, entity configs, external APIs
│   └── ScoreCast.Ws.Services          → Cross-cutting business services
├── src/Shared/
│   ├── ScoreCast.ApiClient            → Refit API interfaces
│   ├── ScoreCast.Models               → Request/response records
│   └── ScoreCast.Shared               → Constants, enums, extensions, types
└── src/Web/
    ├── ScoreCast.Web                  → Blazor WASM standalone app (pages, layout, theme)
    ├── ScoreCast.Web.Server           → ASP.NET Core host for WASM (port 5200)
    └── ScoreCast.Web.Components       → Razor class library (reusable components, helpers)
```

## Dependency Flow
```
Ws (host) → Endpoints → Application → Domain → Shared
                ↑              ↑
            Services     Infrastructure → Application → Domain
                                ↓
                          Shared libs

Web.Server → Web (WASM) → Web.Components → Models, ApiClient
```

## Key Domain Entities
- **UserMaster** — user profile, total points, streaks, Keycloak ID
- **Competition / Season / Gameweek** — football hierarchy
- **Team / Player / TeamPlayer / SeasonTeam** — squad management
- **Match / MatchEvent** — fixtures with scores, status, minute, goal/card events
- **Prediction** — per-user per-season per-match scoreline predictions
- **PredictionLeague / PredictionLeagueMember** — leagues with invite codes
- **PredictionScoringRule** — configurable scoring rules (points computed on the fly)
- **ExternalMapping** — maps internal entities to external API IDs (Pulse, FPL, FootballData)

## External Data Sources
- **Pulse API** (primary for PL) — fixtures, scores, clock, events, teams
- **Football-data.org** (secondary) — fallback for non-PL or when Pulse fails
- **FPL API** — player data enrichment, Pulse ID mappings
- Pulse compSeason mapping stored in `external_mapping` table (e.g. Season 474 → Pulse 777)

## Key Pages
- `/` — landing page (anonymous) or redirect to `/leagues` (authenticated)
- `/leagues` — prediction tile + league list
- `/leagues/{id}` — league detail with standings
- `/predict` — submit predictions (season-scoped)
- `/scores` — live scores with match minute, goal scorers
- `/points-table` — standings with competition zones (league) or group tables (World Cup)
- `/player-stats` — sortable player statistics
- `/master-data-sync` — admin data sync operations

## Running Locally
1. `docker compose up -d` (Keycloak + DBs)
2. Configure user secrets (Neon connection string, Keycloak settings)
3. Run `ScoreCast.Ws` (port 5105) + `ScoreCast.Web.Server` (port 5200)
4. Do NOT run `ScoreCast.Web` directly (WasmAppHost bug in .NET 10.0.101)
