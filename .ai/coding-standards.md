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
- **Never use `Route<T>()` in endpoints** — always bind from request model properties
- **Models** — request/response records in `ScoreCast.Models`
- **Constants** in `ScoreCast.Shared.Constants`, enums in `ScoreCast.Shared.Enums`
- Entity configurations (EF Fluent API) go in `Infrastructure/`
- No magic strings — use constants

## CQRS Conventions
- Queries: `public record XxxQuery(...) : IQuery<ScoreCastResponse<T>>;`
- Query Handlers: `internal sealed record XxxQueryHandler(...) : IQueryHandler<XxxQuery, ScoreCastResponse<T>>`
- Commands: `public record XxxCommand(XxxRequest Request) : ICommand<ScoreCastResponse>;`
- Command Handlers: `internal sealed record XxxCommandHandler(IScoreCastDbContext DbContext, IUnitOfWork UnitOfWork) : ICommandHandler<XxxCommand, ScoreCastResponse>`
- **Every command must take a request** extending `ScoreCastRequest` (or `ScoreCastRequest` directly if no extra props)
- **Use `ScoreCastRequest` directly** when a request has no additional properties — don't create empty subclasses
- **`SaveChangesAsync` must always use `request.AppName ?? nameof(XxxCommand)` pattern**
- **Queries should NEVER write to the database** — if it writes, make it a command
- **Use `required string` in request models**, not `= null!`
- Commands/Queries are `public`, handlers are `internal sealed`

## FastEndpoints Conventions
- Use `Send.OkAsync()` for sending responses
- Endpoints inherit `EndpointWithoutRequest<TResponse>` or `Endpoint<TRequest, TResponse>`
- `AllowAnonymous()` ONLY for: health check, landing page, login, sign up, about, contact us
- POST endpoints: all data from `[Body]` request model — no route `{parameter}` placeholders
- GET endpoints: route parameters as method parameters
- `UserId` auto-populated by `FirebaseUserPreprocessor` — never set manually in endpoints
- API key clients get `UserId` set to client name (e.g., `ScoreCast.Jobs`)

## Code Style
- **Records everywhere** for commands, queries, handlers, DTOs, and value objects with primary constructors
- No magic strings — use constants classes or `nameof(EnumValue)`
- No hardcoded URLs, status codes, or API values — everything via constants
- Use `ScoreCastDateTime` instead of `DateTime` in models and responses
- Points NEVER stored — computed on the fly from `Outcome` + `scoring_rules` table
- Catch blocks that only log are intentional for enrichment steps — partial success preferred
- Use expression-bodied members for single-line methods/properties
- Keep methods short — extract a method if a block needs a comment explaining it
- **No two records in the same file** (except related DTOs)

## Database
- Entity configs MUST use `HasColumnOrder` with incrementing `var order = 1;` pattern
- Snake_case for all column names
- Global query filter for soft delete: `builder.HasQueryFilter(q => !q.IsDeleted)`
- No EF attributes/annotations on Domain entities — Fluent API only
- When creating entities, don't use `required` on FK IDs — just use `long` and pass actual entity references
- Use `EF.Functions.ILike` for case-insensitive search on PostgreSQL

## Blazor / Frontend
- No `@code` blocks in `.razor` files — always use code-behind `.razor.cs` files
- All API client calls wrapped in `Loading.While`
- **ViewModels go in `ScoreCast.Web/ViewModels/{Feature}/`**
- **Validators go in `ScoreCast.Web/Validation/{Feature}/`** using FluentValidation + `AbstractValidator<T>`
- Pages inject Refit interfaces directly (no controller layer)
- Use `IAlertService` with `Alert.Add(message, Severity.X)` for notifications — **no `ISnackbar`**
- No `InvokeAsync` wrappers in Blazor WASM pages (only needed in Blazor Server)
- Light mode only — no dark mode toggle or `PaletteDark`
- Mobile: pill tabs with dark background, `MudSimpleTable` with tight padding
- Desktop: `MudTabs` with `Header` render fragment for inline search bars
- Use `var(--mud-palette-text-secondary)` for secondary text — **never use `opacity` for text dimming**

## Refit API Client
- POST methods: `Task<ScoreCastResponse> MethodAsync([Body] RequestType request, CancellationToken ct)`
- GET methods: route parameters as method parameters
- Auth token attached via `FirebaseTokenHandler` (DelegatingHandler)

## External APIs
- **Pulse** is primary data source for Premier League
- **Football-data.org** is fallback for non-PL or when Pulse fails
- **FPL API** used for Pulse ID mappings
- New Pulse endpoints added to `PulseApi.Routes` constants
- Pulse response models use `[property: JsonPropertyName]` for case-sensitive deserialization

## Dependencies
- Central Package Management via `Directory.Packages.props` — never specify versions in individual `.csproj` files
- FastEndpoints for endpoints and CQRS — no MediatR
- EF Core for data access
