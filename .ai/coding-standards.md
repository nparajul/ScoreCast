# Coding Standards

## General
- Target .NET 10, C# latest
- `TreatWarningsAsErrors` is enabled — no warnings allowed
- File-scoped namespaces everywhere
- Nullable reference types enabled
- Write only the ABSOLUTE MINIMAL amount of code needed

## Naming
- Private fields: `_camelCase`
- Public properties: `PascalCase`
- Interfaces: `I` prefix (e.g., `IMatchRepository`)
- Async methods: `Async` suffix
- Snake_case for all database column names

## Architecture Rules
- **Domain** must have zero external dependencies (no NuGet packages, no project references except Shared)
- **Application** defines commands, queries, and interfaces only — no implementations
- **Infrastructure** implements all handlers and data access
- **Endpoints** are thin — delegate to commands/queries, no business logic
- **Models** — request/response records in `ScoreCast.Models`
- **Constants** in `ScoreCast.Shared.Constants`, enums in `ScoreCast.Shared.Enums`
- Entity configurations (EF Fluent API) go in `Infrastructure/`

## CQRS Conventions
- Queries: `public record XxxQuery(...) : IQuery<ScoreCastResponse<T>>;` (from `ScoreCast.Ws.Application.V1.Interfaces`)
- Query Handlers: `internal sealed record XxxQueryHandler(...) : IQueryHandler<XxxQuery, ScoreCastResponse<T>>`
- Commands: `public record XxxCommand(XxxRequest Request) : ICommand<ScoreCastResponse>;`
- Command Handlers: `internal sealed record XxxCommandHandler(IScoreCastDbContext DbContext, IUnitOfWork UnitOfWork) : ICommandHandler<XxxCommand, ScoreCastResponse>`
- `IQuery<T>` extends `ICommand<T>`, `IQueryHandler<TQuery, TResult>` extends `ICommandHandler<TQuery, TResult>` — defined in `ScoreCast.Ws.Application.V1.Interfaces`
- Commands/Queries are `public`, handlers are `internal sealed`
- `SaveChangesAsync` must always use `request.AppName ?? nameof(XxxCommand)` pattern

## FastEndpoints Conventions
- Use `Send.OkAsync()` for sending responses
- Endpoints inherit `EndpointWithoutRequest<TResponse>` or `Endpoint<TRequest, TResponse>`
- `AllowAnonymous()` ONLY for: health check, landing page, login, sign up, about, contact us
- POST endpoints: all data from `[Body]` request model — no route `{parameter}` placeholders
- GET endpoints: route parameters as method parameters

## Code Style
- **Records everywhere** for commands, queries, handlers, DTOs, and value objects with primary constructors
- No magic strings — use constants classes or `nameof(EnumValue)`
- No hardcoded URLs, status codes, or API values — everything via constants
- Use `ScoreCastDateTime.Now` instead of `DateTime.UtcNow`
- Points NEVER stored — computed on the fly from `Outcome` + `scoring_rules` table
- Catch blocks that only log are intentional for enrichment steps — partial success preferred
- Use expression-bodied members for single-line methods/properties
- Keep methods short — extract a method if a block needs a comment explaining it

## Database
- Entity configs MUST use `HasColumnOrder` with incrementing `var order = 1;` pattern
- Snake_case for all column names
- Global query filter for soft delete: `builder.HasQueryFilter(q => !q.IsDeleted)`
- No EF attributes/annotations on Domain entities — Fluent API only

## Blazor / Frontend
- No `@code` blocks in `.razor` files — always use code-behind `.razor.cs` files
- All API client calls wrapped in `Loading.While`
- ViewModels in `ScoreCast.Web/ViewModels/`
- Pages inject Refit interfaces directly (no controller layer)
- Use `Alert.Add(message, Severity.X)` for alerts
- Use `ISnackbar` for transient toast notifications
- No `InvokeAsync` wrappers in Blazor WASM pages (only needed in Blazor Server)

## Refit API Client
- POST methods: `Task<ScoreCastResponse> MethodAsync([Body] RequestType request, CancellationToken ct)`
- GET methods: route parameters as method parameters (e.g. `long seasonId, long gameweekId`)
- Auth token attached via `BaseAddressAuthorizationMessageHandler`

## External APIs
- **Pulse** is primary data source for Premier League
- **Football-data.org** is fallback for non-PL or when Pulse fails
- **FPL API** used for Pulse ID mappings
- New Pulse endpoints added to `PulseApi.Routes` constants
- Pulse response models use `[property: JsonPropertyName]` for case-sensitive deserialization
- If FPL sync fails, Pulse sync should NOT run (sequential gating)
- Enhance live matches should pull from ALL available sources

## Dependencies
- Central Package Management via `Directory.Packages.props` — never specify versions in individual `.csproj` files
- FastEndpoints for endpoints and CQRS — no MediatR
- EF Core for data access
- `UserId` auto-populated by `KeycloakUserPreprocessor` — never set manually in endpoints
