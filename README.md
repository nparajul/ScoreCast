# ScoreCast

A Premier League predictions app built with .NET 10, clean architecture, and FastEndpoints (CQRS).

## Projects

### APIs
- **ScoreCast.Ws** — API host, DI registration, middleware, startup
- **ScoreCast.Ws.Application** — Commands, queries, interfaces
- **ScoreCast.Ws.Domain** — Entities, value objects, domain logic
- **ScoreCast.Ws.Endpoints** — FastEndpoints endpoint definitions
- **ScoreCast.Ws.Infrastructure** — Command handlers, query handlers, EF Core, external API integrations
- **ScoreCast.Ws.Services** — Cross-cutting business services

### Shared
- **ScoreCast.ApiClient** — Typed HTTP client
- **ScoreCast.Models** — Shared DTOs and contracts
- **ScoreCast.Shared** — Cross-cutting utilities

## Architecture

```
Endpoints → Application → Domain
                ↑
          Infrastructure (implements FastEndpoints command/query handlers)
```

## Tech Stack
- .NET 10
- FastEndpoints (REPR pattern + CQRS)
- Central Package Management
