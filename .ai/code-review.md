# Code Review Checklist

## Architecture
- [ ] No business logic in Endpoints — they should only map requests to commands/queries
- [ ] Domain entities have no dependencies on Infrastructure or Application
- [ ] New NuGet packages added to `Directory.Packages.props`, not individual csproj files
- [ ] No circular project references
- [ ] Commands: `public record`, Handlers: `internal sealed record`
- [ ] Every command takes a request extending `ScoreCastRequest`
- [ ] Queries use `IQuery<T>` / `IQueryHandler` — queries NEVER write to the database
- [ ] Commands use `ICommand<T>` / `ICommandHandler` — use for any write operation
- [ ] Response records in `ScoreCast.Models`, requests in `ScoreCast.Models`
- [ ] Constants in `ScoreCast.Shared.Constants`, enums in `ScoreCast.Shared.Enums`
- [ ] Never use `Route<T>()` — bind from request model properties

## Code Quality
- [ ] No compiler warnings (TreatWarningsAsErrors is on)
- [ ] Nullable reference types handled properly — no `!` operator unless justified
- [ ] Async all the way — no `.Result` or `.Wait()` calls
- [ ] No magic strings — use constants classes or `nameof()`
- [ ] No hardcoded URLs, status codes, or API values
- [ ] Records with primary constructors for commands, queries, handlers, DTOs
- [ ] `SaveChangesAsync` uses `request.AppName ?? nameof(XxxCommand)` pattern
- [ ] Use `ScoreCastDateTime` not `DateTime` in models/responses
- [ ] No two records in the same file (except related DTOs)
- [ ] Use `required string` in request models, not `= null!`

## Naming & Style
- [ ] File-scoped namespaces used
- [ ] Private fields use `_camelCase`
- [ ] Async methods suffixed with `Async`
- [ ] Snake_case for all database column names
- [ ] Entity configs use `HasColumnOrder` with incrementing `var order = 1;` pattern

## Data Access
- [ ] Entity configurations use Fluent API in `Infrastructure/`
- [ ] No EF attributes/annotations on Domain entities
- [ ] Queries use `AsNoTracking()` where appropriate
- [ ] Points NEVER stored — computed on the fly
- [ ] Use `EF.Functions.ILike` for case-insensitive search on PostgreSQL

## Endpoints & API
- [ ] POST endpoints: all data from `[Body]` request model — no route parameters
- [ ] GET endpoints: route parameters as method parameters
- [ ] POST Refit methods: `[Body] RequestType` signature
- [ ] `UserId` auto-populated by `FirebaseUserPreprocessor` — never set manually
- [ ] API key clients get `UserId` from client name

## Blazor / Frontend
- [ ] No `@code` blocks in `.razor` files — always code-behind
- [ ] All API client calls wrapped in `Loading.While`
- [ ] ViewModels in `ScoreCast.Web/ViewModels/{Feature}/`
- [ ] Validators in `ScoreCast.Web/Validation/{Feature}/` using FluentValidation
- [ ] Use `IAlertService` — never `ISnackbar`
- [ ] Light mode only — no dark mode references
- [ ] Never use `opacity` for text dimming — use `var(--mud-palette-text-secondary)`
- [ ] Mobile tables use `MudSimpleTable` with tight padding
- [ ] Desktop tables use `MudTable` with sorting, paging

## Security
- [ ] No secrets in code or config files
- [ ] Input validation on all endpoints
- [ ] `AllowAnonymous` only for: health check, landing page, login, sign up, about, contact us
- [ ] Firebase API key is public (client-side) — this is by design
