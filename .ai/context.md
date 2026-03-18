# ScoreCast — Project Context

## Overview
Football predictions app where users predict match outcomes and scorelines across multiple competitions (Premier League, FIFA World Cup 2026). Points awarded based on accuracy with leaderboards across prediction leagues. Built with .NET 10 and clean architecture. Light mode only. Currently in beta.

## Tech Stack
- .NET 10 (SDK 10.0.101), C# latest
- FastEndpoints 8.0.1 (REPR pattern + CQRS, no MediatR)
- Entity Framework Core 10.0.4 with Npgsql (Neon cloud PostgreSQL)
- Blazor WASM (standalone) with MudBlazor 9.1.0
- Firebase Auth (Email/Password + Google Sign-In) via JS interop
- Refit 10.0.1 — typed HTTP API clients
- Serilog — structured logging
- FluentValidation — form validation with MudBlazor bridge
- Central Package Management, `.slnx` solution format

## Hosting
- **Frontend**: Cloudflare Pages (deployed from `cloudfare-dev` branch)
- **API**: Render (free tier, auto-sleep)
- **Database**: Neon cloud PostgreSQL
- **Auth**: Firebase (Google Cloud)
- **Sync Jobs**: GitHub Actions (scheduled cron jobs)

## Authentication
- **Frontend**: Firebase JS SDK via `firebase-auth.js` interop. `ScoreCastAuthStateProvider` manages auth state. `FirebaseTokenHandler` (DelegatingHandler) attaches Firebase ID tokens to API requests. Persistent auth via IndexedDB.
- **Backend**: Firebase JWT validation from `securetoken.google.com/{projectId}`. `FirebaseUserPreprocessor` extracts user identity and populates `ScoreCastRequest.UserId`.
- **API Key Auth**: For GitHub Actions sync jobs. `ApiKeyAuthHandler` validates `X-Api-Key` header against `ApiKeySettings:Clients` config. Sets `UserId` to client name (e.g., `ScoreCast.Jobs`).
- **Firebase config**: API key is public (client-side by design). `ProjectId` in user secrets / Render env vars.

## Architecture
- **CQRS without MediatR** — commands/queries in Application, handlers in Infrastructure via FastEndpoints
- **Queries** use `IQuery<T>` / `IQueryHandler<TQuery, TResult>` interfaces — NEVER write to the database
- **Commands** use `ICommand<T>` / `ICommandHandler<TCommand, TResult>` from FastEndpoints — use for any operation that writes
- **Every command takes a request** extending `ScoreCastRequest` (or `ScoreCastRequest` directly if no extra props) for `AppName` and `UserId` audit fields
- **Clean Architecture** — Domain has zero dependencies (except Shared), Application depends on Domain, Infrastructure implements Application interfaces

## Project Structure
```
Solution
├── .ai/                               → AI agent context, coding standards, review checklist
├── .kiro/rules/                       → Kiro CLI rules (git workflow)
├── .github/workflows/                 → Deploy web (Cloudflare), sync jobs (GitHub Actions)
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
    ├── ScoreCast.Web                  → Blazor WASM standalone app (pages, layout, theme, auth)
    ├── ScoreCast.Web.Server           → ASP.NET Core host for WASM (port 5200)
    └── ScoreCast.Web.Components       → Razor class library (reusable components, helpers)
```

## Key File Locations
- Firebase JS interop: `src/Web/ScoreCast.Web/wwwroot/js/firebase-auth.js`
- Auth state provider: `src/Web/ScoreCast.Web/Auth/ScoreCastAuthStateProvider.cs`
- Auth models: `src/Web/ScoreCast.Web/Auth/AuthResult.cs`, `FirebaseModels.cs`
- Firebase constants: `src/Shared/ScoreCast.Shared/Constants/FirebaseAuth.cs`
- API auth: `src/APIs/ScoreCast.Ws/Extensions/AuthenticationExtensions.cs`
- Preprocessor: `src/APIs/ScoreCast.Ws.Endpoints/Preprocessors/FirebaseUserPreprocessor.cs`
- Validation bridge: `src/Web/ScoreCast.Web/Validation/FluentValidatorExtensions.cs`
- ViewModels: `src/Web/ScoreCast.Web/ViewModels/{Feature}/`
- Validators: `src/Web/ScoreCast.Web/Validation/{Feature}/`

## Key Domain Entities
- **UserMaster** — user profile, total points, streaks, Firebase UID (stored in `KeycloakUserId` column — rename deferred)
- **Competition / Season / Gameweek** — football hierarchy
- **Team / Player / TeamPlayer / SeasonTeam** — squad management
- **Match / MatchEvent** — fixtures with scores, status, minute, goal/card events
- **MatchLineup** — starters and substitutes from Pulse TeamLists data
- **MatchInsightCache** — AI-generated match insights cached per gameweek
- **Prediction** — per-user per-season per-match scoreline predictions
- **PredictionLeague / PredictionLeagueMember** — leagues with invite codes
- **PredictionScoringRule** — configurable scoring rules (points computed on the fly)
- **ExternalMapping** — maps internal entities to external API IDs (Pulse, FPL, FootballData)

## Key Pages
- `/` — landing page (anonymous) with features, FAQ, competitions, beta badge
- `/login` — email/password + Google Sign-In
- `/register` — email/password registration with FluentValidation
- `/dashboard` — prediction tile + league list ("Predict" in bottom nav)
- `/predict` — submit predictions (season-scoped)
- `/scores` — live scores with match events
- `/points-table` — league standings or group tables + knockout bracket
- `/player-stats` — stats with pill tabs (mobile) or tabbed view (desktop)
- `/teams` — team grid with search, pagination (Load More)
- `/teams/{id}` — team detail
- `/insights` — AI-powered match previews
- `/settings` — user profile (display name, favourite team autocomplete, member since, stats, logout confirmation)
- `/master-data-sync` — admin data sync operations

## Mobile UI
- Floating dark glass pill bottom nav with emoji icons (🏆 Predict, 🛡️ Teams) and colored Material icons
- No hamburger menu — bottom nav covers all navigation
- Fullscreen dialogs on mobile for keyboard-safe dropdowns
- Floating toast alerts (fixed position, visible while scrolled)
- Footer hidden on mobile
- WelcomeDialog: rounded glassmorphism design with team autocomplete

## Theme
- Light mode only
- Primary: `#0A1929` (navy), Secondary: `#37003C` (PL purple), Tertiary: `#FF6B35` (orange)
- TextSecondary: `#555555`
- AppBar: navy with white text

## Git Workflow
- **Never** commit directly to `master` — always feature branches + PRs
- **Never** touch `cloudfare-dev` via Kiro — managed externally
- Feature branches: `feature/xxx`, `fix/xxx`, `chore/xxx`

## Running Locally
1. Configure user secrets (Neon connection string, `Firebase:ProjectId`, API keys)
2. Run `ScoreCast.Ws` (port 5105) + `ScoreCast.Web.Server` (port 5200)
3. Do NOT run `ScoreCast.Web` directly (WasmAppHost bug in .NET 10.0.101)
