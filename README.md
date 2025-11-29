# ScoreCast

A Premier League predictions app where users predict match outcomes, scorelines, and goal scorers. Points are awarded based on prediction accuracy with leaderboards, streaks, and head-to-head challenges.

## Tech Stack

- **.NET 10** (SDK 10.0.101) with latest C# features
- **FastEndpoints 8.0.1** — REPR pattern, CQRS without MediatR
- **Entity Framework Core 10.0.4** with **Npgsql** (Neon cloud PostgreSQL)
- **Keycloak 26.1** — self-hosted OIDC/OAuth2 identity provider (Docker)
- **Blazor WASM** (standalone) with **MudBlazor 9.1.0**
- **Refit 10.0.1** — typed HTTP API clients
- **Serilog** — structured logging (console + rolling file)
- **OneOf 3.0.271** — discriminated unions for alert/UI services
- Central Package Management, `.slnx` solution format

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

Test users: `admin`/`admin`, `testuser`/`test`

## Authentication

- Keycloak issues JWT tokens via OIDC
- API validates with JWT Bearer (`scorecast-api` audience)
- Blazor WASM uses OIDC with `scorecast-web` client
- Custom `ApiAuthHandler` (DelegatingHandler) attaches Bearer tokens to Refit API calls
- `KeycloakUserPreprocessor` extracts `sub` claim server-side, populates `UserId` on requests

## Branding & Theme

### Colors
| Role | Light Mode | Dark Mode |
|---|---|---|
| Primary | `#0A1929` (deep navy) | `#8B1A9E` (purple) |
| Secondary | `#37003C` (PL purple) | `#FF6B35` (orange) |
| Tertiary | `#FF6B35` (orange) | `#8B1A9E` (purple) |
| Background | `#F5F7FA` (light grey) | `#0A1929` (navy) |
| Surface | `#FFFFFF` (white) | `#132F4C` (dark blue) |

### Logo
- `scorecast-logo.svg` — full logo (football icon + "ScoreCast" text + tagline) for light backgrounds
- `scorecast-logo-dark.svg` — white text variant for dark backgrounds
- `scorecast-icon.svg` — compact square icon (favicon, loading splash)
- `scorecast-icon-light.svg` — backgroundless variant (app bar)
- Logo swaps automatically based on dark/light mode

### Keycloak Custom Theme
Located at `keycloak/themes/scorecast/login/` — custom login and register pages matching the app branding, mounted via Docker volume.

## Key Features (Built)

- User registration and login via Keycloak
- Automatic user sync on first login (Blazor → API)
- User profile management (GET/PUT /api/v1/users/me)
- Dark/light mode toggle with theme-aware logos
- Custom branded loading splash
- SVG favicon
- Sticky footer
- Styled logged-out page
- Custom Keycloak login/register theme

## What's Not Built Yet

- Match, Prediction, Gameweek, League, Leaderboard entities
- Prediction submission and scoring logic
- Leaderboard calculations
- Private leagues
- Background jobs
- External football data API integration
- Keycloak setup automation script
- Profile editing page
- Dashboard page for authenticated users
