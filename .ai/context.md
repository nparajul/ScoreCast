# ScoreCast — Project Context

## Overview
Football predictions app where users predict match outcomes and scorelines across multiple competitions (Premier League, FIFA World Cup 2026). Points awarded based on accuracy with leaderboards across prediction leagues. Built with .NET 10 and clean architecture. Light mode only.

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
- **MatchLineup** — tracks match starters (is_starter=true) and substitutes (is_starter=false) from Pulse TeamLists data
- **Prediction** — per-user per-season per-match scoreline predictions
- **PredictionLeague / PredictionLeagueMember** — leagues with invite codes
- **PredictionScoringRule** — configurable scoring rules (points computed on the fly)
- **ExternalMapping** — maps internal entities to external API IDs (Pulse, FPL, FootballData)

## External Data Sources
- **Pulse API** (primary for PL) — fixtures, scores, clock, events, teams, lineups
- **Football-data.org** (secondary) — fallback for non-PL or when Pulse fails
- **FPL API** — player data enrichment, Pulse ID mappings
- Pulse compSeason mapping stored in `external_mapping` table (e.g. Season 474 → Pulse 777)

## Data Sync Schedule
- **EnhanceLive**: every 60-90s (safe 24/7, no-ops when no live matches)
- **Sync Matches** (football-data.org): every 5 min (rate limit: 10 req/min free tier)
- **Calculate Predictions**: every 5 min after Sync Matches
- **FPL + Pulse Events**: once after all matches finish (post-match)
- All can run 24/7 — they short-circuit when nothing to do

## Key Pages
- `/` — landing page with branding, features, competitions (anonymous) or redirect to `/leagues` (authenticated)
- `/leagues` — prediction tile + league list
- `/leagues/{id}` — league detail with standings
- `/predict` — submit predictions (season-scoped)
- `/scores` — live scores with match events (goals, cards, subs with colored arrows, referee whistle icon). Desktop has larger text/logos.
- `/points-table` — league standings with competition zones, or group tables + best 3rd placed + knockout bracket (World Cup). Mobile uses pill tabs + FotMob-style bracket with swipe navigation.
- `/player-stats` — mobile: pill tabs (Overall/Goals/Assists/Clean Sheets/Discipline). Desktop: Outfield + Clean Sheets tabs with search bar in tab header. Position abbreviations shown next to player names.
- `/master-data-sync` — admin data sync operations

## Player Stats — Clean Sheet Rules
- GK started, not subbed off → clean sheet if team conceded 0
- GK started, subbed off ≥ 60' → clean sheet if team conceded 0
- GK started, subbed off < 60' → no clean sheet
- GK subbed on < 30' → clean sheet if opponent scored 0 goals after sub-on minute
- GKs with 0 match events but clean sheets are included via MatchLineup data

## Player Positions
- `PlayerPositions` constants class in `ScoreCast.Shared.Constants`
- 15 positions from DB + `ToShortName()` mapping (e.g. Centre-Back → CB, Defence → CB, Midfield → CM, Offence → CF)

## Mobile UI Patterns
- Pill tabs: dark background rounded container with flex items, `0.65rem` font
- Tables: `MudSimpleTable` with tight padding, emoji headers for stats columns
- Knockout bracket: FotMob-style two-column layout (65% current round + 35% next round preview with opacity), CSS connector lines, swipe navigation with slide animation
- Scores: absolute-positioned minute marker, colored sub arrows (green ▲ / red ▼)

## Theme
- Light mode only (dark mode removed)
- Primary: `#0A1929` (navy), Secondary: `#37003C` (PL purple), Tertiary: `#FF6B35` (orange)
- AppBar: navy with white text

## Running Locally
1. `docker compose up -d` (Keycloak + DBs)
2. Configure user secrets (Neon connection string, Keycloak settings)
3. Run `ScoreCast.Ws` (port 5105) + `ScoreCast.Web.Server` (port 5200)
4. Do NOT run `ScoreCast.Web` directly (WasmAppHost bug in .NET 10.0.101)
