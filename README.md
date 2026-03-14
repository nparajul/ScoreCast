# ScoreCast

A Premier League predictions app where users predict match outcomes and scorelines. Points are awarded based on prediction accuracy with leaderboards across prediction leagues.

## Tech Stack

- **.NET 10** (SDK 10.0.101) with latest C# features
- **FastEndpoints 8.0.1** — REPR pattern, CQRS without MediatR
- **Entity Framework Core 10.0.4** with **Npgsql** (Neon cloud PostgreSQL)
- **Keycloak 26.1** — self-hosted OIDC/OAuth2 identity provider (Docker)
- **Blazor WASM** (standalone) with **MudBlazor 9.1.0**
- **Refit 10.0.1** — typed HTTP API clients
- **Serilog** — structured logging (console + rolling file)
- Central Package Management, `.slnx` solution format

## Key Features

### Predictions & Scoring
- Predict match outcomes and scorelines for every Premier League match
- Predictions lock at kickoff — no changes after a match starts
- Points computed on the fly from configurable scoring rules (never stored)
- Scoring: Exact Score (10pts), Correct Result + GD (7pts), Correct Result (5pts), Correct GD (3pts)

### Prediction Leagues
- Create and join prediction leagues with invite codes
- League standings with real-time point calculations
- Predictions are per-season, shared across all leagues a user belongs to

### Live Match Experience
- Real-time match minute, scores, and status from Pulse API
- Goal scorers, assists, cards, and substitutions synced live
- Live match cards highlighted with visual indicators

### Data Sources (Premier League)
- **Pulse API** (primary) — official PL API for fixtures, scores, clock, events, teams
- **Football-data.org** (secondary) — fallback for non-PL or when Pulse fails
- **FPL API** — used for Pulse ID mappings and player data enrichment

### Other
- Google Sign-In via Keycloak identity provider
- Dark/light mode with custom branded theme
- League table with competition zone color coding
- Player stats with sortable columns
- Admin data sync page with categorized operations

## Projects

### APIs
| Project | Purpose |
|---|---|
| **ScoreCast.Ws** | API host, DI, middleware, startup (port 5105) |
| **ScoreCast.Ws.Application** | Commands, queries, interfaces |
| **ScoreCast.Ws.Domain** | Entities, value objects, domain logic |
| **ScoreCast.Ws.Endpoints** | FastEndpoints definitions, preprocessors |
| **ScoreCast.Ws.Infrastructure** | Handlers, DbContext, entity configs |
| **ScoreCast.Ws.Services** | Cross-cutting business services |

### Web
| Project | Purpose |
|---|---|
| **ScoreCast.Web** | Blazor WASM standalone app (pages, layout, theme) |
| **ScoreCast.Web.Server** | ASP.NET Core host for WASM (port 5200) |
| **ScoreCast.Web.Components** | Razor class library (reusable components, helper services) |

### Shared
| Project | Purpose |
|---|---|
| **ScoreCast.ApiClient** | Refit API interfaces |
| **ScoreCast.Models** | Request/response records |
| **ScoreCast.Shared** | Constants, enums, extensions, types |

## Architecture

```
Ws (host) → Endpoints → Application → Domain → Shared
                ↑              ↑
            Services     Infrastructure → Application → Domain
                                ↓
                          Shared libs

Web.Server → Web (WASM) → Web.Components → Models, ApiClient
```

## Running Locally

### Prerequisites
- .NET 10 SDK
- Docker & Docker Compose
- Rider or VS (compound run config recommended)

### 1. Start Docker services
```bash
cp .env.example .env   # fill in passwords
docker compose up -d
```

This starts:
- `keycloak-db` — Postgres 17 (Keycloak data)
- `keycloak` — Keycloak 26.1 (port 8080)
- `scorecast-db` — Postgres 17 (port 5432, legacy — app uses Neon cloud)

### 2. Configure user secrets
```bash
cd src/APIs/ScoreCast.Ws
dotnet user-secrets set "ConnectionStrings:ScoreCastDb" "<neon-connection-string>"
dotnet user-secrets set "Keycloak:Authority" "http://localhost:8080/realms/scorecast"
dotnet user-secrets set "Keycloak:Audience" "scorecast-api"
```

### 3. Run the app
Run both projects (Rider compound config recommended):
- **ScoreCast.Ws** → API on port 5105
- **ScoreCast.Web.Server** → Blazor WASM on port 5200

> Do NOT run `ScoreCast.Web` directly — there's a .NET 10.0.101 WasmAppHost bug with fingerprinted framework files.

### 4. Keycloak post-restart config
After `docker compose down/up`, re-apply via admin API:
1. Set login theme to `scorecast`
2. Add `localhost:5200` to redirect URIs and web origins for `scorecast-web` client
3. Ensure `openid`, `profile`, `email` client scopes are assigned as defaults
4. Set `firstName`/`lastName` to optional in user profile
5. Add Google identity provider (Client ID + Secret from Google Cloud Console — not committed to git)

Test users: `admin`/`admin`, `testuser`/`test`

## Data Sync

Admin page (`/master-data-sync`) provides manual sync operations:

| Operation | Source | Description |
|---|---|---|
| Sync Competition | Football-data.org | Competitions, seasons, countries |
| Sync Teams | Pulse (PL) / Football-data.org | Teams, players, season rosters |
| Sync Matches | Pulse (PL) / Football-data.org | All fixtures with scores and status |
| Sync FPL Data | FPL API | Player mappings, Pulse ID mappings |
| Sync Pulse Events | Pulse API | Match events (goals, cards, subs) |
| Enhance Live | Pulse + Football-data.org | Real-time scores, clock, events for live matches |
| Calculate Points | — | Compute outcomes and accumulate user points |

### New Season Setup
When a new PL season starts:
1. Run Sync Competition (creates new season, sets `IsCurrent`)
2. Find new Pulse compSeason ID from any fixture's `gameweek.compSeason.id`
3. Insert mapping: `external_mapping(EntityType=Season, Source=Pulse, ExternalCode=<id>)`
4. Run Sync Teams + Sync Matches as normal

## Authentication

- Keycloak issues JWT tokens via OIDC
- API validates with JWT Bearer (`scorecast-api` audience)
- Blazor WASM uses OIDC with `scorecast-web` client
- `KeycloakUserPreprocessor` extracts `sub` claim server-side, populates `UserId` on requests
- Custom Keycloak login/register theme matching app branding
