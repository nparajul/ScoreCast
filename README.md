# ScoreCast

A football predictions app where users predict match outcomes and scorelines across multiple competitions. Points are awarded based on prediction accuracy with leaderboards across prediction leagues. Currently in beta.

## Tech Stack

- **.NET 10** (SDK 10.0.101) with latest C# features
- **FastEndpoints 8.0.1** — REPR pattern, CQRS without MediatR
- **Entity Framework Core 10.0.4** with **Npgsql** (Neon cloud PostgreSQL)
- **Firebase Auth** — Email/Password + Google Sign-In
- **Blazor WASM** (standalone) with **MudBlazor 9.1.0**
- **Refit 10.0.1** — typed HTTP API clients
- **FluentValidation** — form validation with MudBlazor bridge
- **Serilog** — structured logging (console + rolling file)
- Central Package Management, `.slnx` solution format

## Hosting

| Service | Platform |
|---|---|
| Frontend | Cloudflare Pages |
| API | Render (free tier) |
| Database | Neon cloud PostgreSQL |
| Auth | Firebase (Google Cloud) |
| Sync Jobs | GitHub Actions (scheduled cron) |

## Key Features

### Predictions & Scoring
- Predict exact scorelines for every match across supported competitions
- Predictions lock at kickoff — no changes after a match starts
- Points computed on the fly from configurable scoring rules (never stored)
- Scoring: Exact Score (10pts), Correct Result + GD (7pts), Correct Result (5pts), Correct GD (3pts)

### Prediction Leagues
- Create and join prediction leagues with invite codes
- League standings with real-time point calculations
- Predictions are per-season, shared across all leagues

### Live Scores
- Real-time match minute, scores, and status from Pulse API
- Goal scorers, assists, cards, and substitutions synced live

### AI Insights
- AI-generated match previews with form analysis and predicted outcomes
- Auto-generated daily via GitHub Actions cron job

### Supported Competitions
- Premier League (full 38-matchweek season)
- FIFA World Cup 2026 (groups + knockout bracket)
- More on the roadmap

### Other
- Google Sign-In + email/password via Firebase
- Team profiles with squads, results, fixtures
- Player stats (goals, assists, clean sheets, discipline)
- League tables with competition zone color coding and form guides
- Mobile-first floating glass pill navigation

## Projects

### APIs
| Project | Purpose |
|---|---|
| **ScoreCast.Ws** | API host, DI, middleware, startup (port 5105) |
| **ScoreCast.Ws.Application** | Commands, queries, interfaces |
| **ScoreCast.Ws.Domain** | Entities, value objects |
| **ScoreCast.Ws.Endpoints** | FastEndpoints definitions, preprocessors |
| **ScoreCast.Ws.Infrastructure** | Handlers, DbContext, entity configs, external APIs |
| **ScoreCast.Ws.Services** | Cross-cutting business services |

### Web
| Project | Purpose |
|---|---|
| **ScoreCast.Web** | Blazor WASM app (pages, layout, theme, auth) |
| **ScoreCast.Web.Server** | ASP.NET Core host for WASM (port 5200) |
| **ScoreCast.Web.Components** | Razor class library (reusable components, helpers) |

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

## Authentication

- **Frontend**: Firebase JS SDK via interop. `ScoreCastAuthStateProvider` manages auth state. `FirebaseTokenHandler` attaches ID tokens to API requests. Persistent login via IndexedDB.
- **Backend**: Firebase JWT validation (`securetoken.google.com/{projectId}`). `FirebaseUserPreprocessor` extracts user identity and populates `ScoreCastRequest.UserId`.
- **API Key Auth**: For GitHub Actions sync jobs. `ApiKeyAuthHandler` validates `X-Api-Key` header. Sets `UserId` to client name (e.g., `ScoreCast.Jobs`).
- Firebase API key is public (client-side) — this is by design.

## Running Locally

### Prerequisites
- .NET 10 SDK

### 1. Configure user secrets
```bash
cd src/APIs/ScoreCast.Ws
dotnet user-secrets set "ConnectionStrings:ScoreCastDb" "<neon-connection-string>"
dotnet user-secrets set "Firebase:ProjectId" "<firebase-project-id>"
dotnet user-secrets set "ApiKeySettings:Clients:0:Key" "<api-key>"
dotnet user-secrets set "AI:GitHubToken" "<github-token>"
dotnet user-secrets set "ApiSettings:FootballDataApi:ApiKey" "<football-data-api-key>"
```

### 2. Run the app
Run both projects:
- **ScoreCast.Ws** → API on port 5105
- **ScoreCast.Web.Server** → Blazor WASM on port 5200

> Do NOT run `ScoreCast.Web` directly — there's a .NET 10.0.101 WasmAppHost bug.

## Data Sync

Automated via GitHub Actions (`sync-jobs.yml`):

| Job | Schedule | Description |
|---|---|---|
| Sync Matches | Every 6 hours | Fixtures with scores and status |
| Enhance Live | Every 2 min (12-23 UTC) | Real-time scores for live matches |
| Calculate Points | Every 10 min (12-23 UTC) | Compute outcomes and user points |
| Update Matchday | Every 10 min (12-23 UTC) | Update current matchday per season |
| Generate Insights | Daily 6 AM UTC | AI match previews for upcoming gameweeks |

Manual sync available via admin page (`/master-data-sync`).

All jobs send `appName: "GITHUB-ACTIONS:<JOB-NAME>"` for audit trail.

## Data Sources

| Source | Usage |
|---|---|
| **Pulse API** | Primary for Premier League (fixtures, scores, events, teams, lineups) |
| **Football-data.org** | Fallback for non-PL or when Pulse fails |
| **FPL API** | Player data enrichment, Pulse ID mappings |

## Git Workflow

- Never commit directly to `master` — always feature branches + PRs
- `cloudfare-dev` branch is managed externally (Cloudflare Pages deployment)
- Branch naming: `feature/xxx`, `fix/xxx`, `chore/xxx`
