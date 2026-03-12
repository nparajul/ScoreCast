# ScoreCast — Project Context

## Overview
Premier League predictions app built with .NET 10 and clean architecture.

## Tech Stack
- .NET 10
- FastEndpoints 8.x (REPR pattern + CQRS, no MediatR)
- Entity Framework Core (DbContext in Infrastructure)
- Blazor (frontend — planned)

## Architecture
- **CQRS without MediatR** — commands/queries defined in Application, handlers implemented in Infrastructure using FastEndpoints
- **Clean Architecture** — Domain has zero dependencies, Application depends on Domain, Infrastructure implements Application interfaces
- **Separate Migrations project** — excluded from default build to avoid slow compile times

## Project Structure
```
Solution
├── Migrations/
│   └── ScoreCast.Ws.Migrations       → EF migrations (excluded from default build)
├── src/APIs/
│   ├── ScoreCast.Ws                   → API host, DI, middleware
│   ├── ScoreCast.Ws.Application       → Commands, queries, interfaces
│   ├── ScoreCast.Ws.Domain            → Entities, value objects, domain logic
│   ├── ScoreCast.Ws.Endpoints         → FastEndpoints endpoint definitions
│   ├── ScoreCast.Ws.Infrastructure    → Handlers, DbContext, entity configs, external APIs
│   └── ScoreCast.Ws.Services          → Cross-cutting business services
├── src/Shared/
│   ├── ScoreCast.ApiClient            → Typed HTTP client
│   ├── ScoreCast.Models               → Shared DTOs/contracts
│   └── ScoreCast.Shared               → Cross-cutting utilities
```

## Dependency Flow
```
Ws → Endpoints → Application → Domain
                      ↑
                Infrastructure
```
