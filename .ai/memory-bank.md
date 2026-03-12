# ScoreCast — Architecture Memory Bank

## What Is ScoreCast?
A Premier League predictions app where users predict match outcomes, scorelines, goal scorers, etc. Points awarded based on prediction accuracy. Leaderboards, streaks, head-to-head challenges. Think fantasy football engagement without being a fantasy football copy.

## Tech Decisions Made

### Framework & Runtime
- **.NET 10** with latest C# features
- **FastEndpoints 8.0.1** — REPR pattern, no MVC controllers
- **CQRS without MediatR** — commands/queries defined in Application, handlers in Infrastructure using FastEndpoints
- **Entity Framework Core 10.0.4** with **Npgsql** (PostgreSQL)
- **Serilog** for structured logging (console + rolling file sinks)

### Solution Format
- `.slnx` (new XML-based format) — chosen over `.sln` for readability and clean git diffs

### Authentication & Authorization
- **Keycloak** (self-hosted in Docker) — OIDC/OAuth2 identity provider
- **JWT Bearer** token validation in the API
- Keycloak has its own Postgres DB, separate from app DB
- `AllowAnonymous` only for: health check, landing page, login, sign up, about, contact us
- All other endpoints require authentication

### Docker Setup
- `docker-compose.yml` with 3 services:
  - `scorecast-db` — Postgres 17 (app data, port 5432)
  - `keycloak-db` — Postgres 17 (Keycloak data, internal)
  - `keycloak` — Keycloak 26.1 (port 8080)
- Passwords in `.env` file (gitignored), `.env.example` committed as template
- Sensitive config stored in **user secrets** for local dev

## Project Structure

```
ScoreCast/
├── .ai/                              ← AI agent context (coding standards, review checklist)
├── .env.example                      ← Docker env template (no values)
├── .editorconfig                     ← C# conventions
├── .gitignore
├── Directory.Build.props             ← Shared: net10.0, nullable, implicit usings, TreatWarningsAsErrors
├── Directory.Packages.props          ← Central Package Management (all NuGet versions here)
├── docker-compose.yml
├── nuget.config
├── README.md
├── ScoreCast.slnx
├── Migrations/
│   └── ScoreCast.Ws.Migrations/      ← EF migrations (excluded from default build)
└── src/
    ├── APIs/
    │   ├── ScoreCast.Ws/              ← API host, DI, middleware, startup
    │   ├── ScoreCast.Ws.Application/  ← Commands, queries, interfaces (definitions only)
    │   ├── ScoreCast.Ws.Domain/       ← Entities, value objects, domain logic (zero dependencies)
    │   ├── ScoreCast.Ws.Endpoints/    ← FastEndpoints endpoint definitions + groups
    │   ├── ScoreCast.Ws.Infrastructure/ ← Command/query handlers, DbContext, entity configs, repos
    │   └── ScoreCast.Ws.Services/     ← Cross-cutting business services
    └── Shared/
        ├── ScoreCast.ApiClient/       ← Typed HTTP client (for Blazor → API)
        ├── ScoreCast.Models/          ← Shared DTOs/contracts
        └── ScoreCast.Shared/          ← Cross-cutting utilities (responses, requests)
```

## Dependency Flow

```
Ws (host) → Endpoints → Application → Domain
                ↑              ↑
            Services     Infrastructure → Application → Domain
                                ↓
                          Shared libs
```

- **Domain** has ZERO dependencies — pure entities and logic
- **Application** defines interfaces (`IScoreCastDbContext`, `IUnitOfWork`), commands, queries
- **Infrastructure** implements everything — handlers, DbContext, repos
- **Endpoints** are thin — delegate to commands/queries via FastEndpoints

## Key Design Patterns

### Composite Response Pattern
- `ScoreCastResponse` — base response with `Message`, `ResultType`, `Code`, `ReferenceId`, `Success`
- `ScoreCastResponse<T>` — generic version with `Data` property
- `ScoreCastResponse` constructor is `private protected` — only `ScoreCastResponse<T>` can inherit
- Static factory methods: `Ok()`, `Error()`, `NotFound()`, `Exception()`
- `ScoreCastResultType` enum: `Ok`, `Error`, `NotFound`, `Exception`

### Base Request Pattern
- `ScoreCastRequest` — base record with `AppName` (required), `UserId`, `ReferenceId`
- Not abstract — can be used directly for simple cases (e.g., background jobs)
- Can be inherited for specific request types

### CQRS Pattern
- Commands/queries as `public record` with primary constructors
- Handlers as `internal sealed record` with DI via primary constructors
- Example: `public record AbcCommand(string Name) : ICommand<AbcResponse>;`
- Example: `internal sealed record AbcCommandHandler(IScoreCastDbContext DbContext, IUnitOfWork UnitOfWork) : ICommandHandler<AbcCommand, AbcResponse>`

### Unit of Work
- `IUnitOfWork.SaveChangesAsync(string menuName, string userRole, CancellationToken ct)`
- Only exposed through `IUnitOfWork` interface — not on `IScoreCastDbContext`
- `IScoreCastDbContext` only exposes `Set<T>()` for querying

### Endpoint Groups
- Each feature area has a `Group` class defining route prefix, auth, and tags
- Endpoints attach via `Group<TGroup>()` and define relative routes
- Example: `HealthGroup` at `api/v1/health`, endpoint at `/` → `GET api/v1/health/`

## Program.cs Architecture

### Service Registration (builder side)
1. User secrets (Local environment only)
2. Serilog (config-driven, bootstrap logger for startup errors)
3. FastEndpoints (assembly scanning: Endpoints + Infrastructure)
4. Common services (HTTP logging, CORS, JSON serialization)
5. Authentication (Keycloak JWT Bearer)
6. Infrastructure (DbContext + UnitOfWork)
7. API Versioning (URL segment, query string, header)
8. Swagger (camelCase, enum strings, JWT auth in non-prod)

### Middleware Pipeline (app side)
Single call to `app.ConfigureScoreCastMiddlewares()`:
1. Serilog request logging
2. CORS
3. Authentication + Authorization
4. FastEndpoints (versioning, route prefix `api`, problem details, short names)
5. Swagger (locked down in prod/staging — no try-it-out)
6. Global exception handler → returns `ScoreCastResponse.Exception()`

## Environment Configuration

| Environment | Log Level | Sensitive Data Logging | HTTPS Required | Swagger Try-It |
|---|---|---|---|---|
| Local | Debug | Yes | No | Yes |
| Development | Debug | Yes | No | Yes |
| Staging | Information | No | Yes | No |
| Production | Warning | No | Yes | No |

## Coding Conventions
- File-scoped namespaces
- Private fields: `_camelCase`
- Records everywhere for commands, queries, handlers, DTOs, value objects
- Primary constructors for DI
- `Send.OkAsync()` for endpoint responses (NOT `SendAsync` or `SendOkAsync`)
- Central Package Management — never specify versions in individual csproj files
- `TreatWarningsAsErrors` enabled globally
- Async methods suffixed with `Async`

## NuGet Packages (Central)
| Package | Version | Used In |
|---|---|---|
| FastEndpoints | 8.0.1 | Ws, Endpoints, Infrastructure |
| FastEndpoints.AspVersioning | 8.0.1 | Ws |
| FastEndpoints.Security | 8.0.1 | Ws |
| FastEndpoints.Swagger | 8.0.1 | Ws |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.5 | Ws |
| Microsoft.EntityFrameworkCore | 10.0.4 | Application, Infrastructure |
| Microsoft.EntityFrameworkCore.Design | 10.0.4 | Migrations |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 | Infrastructure |
| Serilog.AspNetCore | 10.0.0 | Ws |
| Serilog.Sinks.Console | 6.1.1 | Ws |
| Serilog.Sinks.File | 7.0.0 | Ws |

## Git Branch Strategy
- `master` — stable base
- `WebService_Initialize` — current working branch (API setup)
- Feature branches for future work

## What's NOT Built Yet
- Domain entities (Match, Prediction, Gameweek, User, League, Leaderboard)
- Blazor frontend
- Keycloak realm configuration
- Actual CQRS commands/queries
- Entity configurations
- Migrations
- Background jobs
- External football data API integration
