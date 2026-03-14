# Code Review Checklist

## Architecture
- [ ] No business logic in Endpoints — they should only map requests to commands/queries
- [ ] Domain entities have no dependencies on Infrastructure or Application
- [ ] New NuGet packages added to `Directory.Packages.props`, not individual csproj files
- [ ] No circular project references
- [ ] Commands: `public record`, Handlers: `internal sealed record`
- [ ] Response result records in `ScoreCast.Models`, requests in `ScoreCast.Models`
- [ ] Constants in `ScoreCast.Shared.Constants`, enums in `ScoreCast.Shared.Enums`

## Code Quality
- [ ] No compiler warnings (TreatWarningsAsErrors is on)
- [ ] Nullable reference types handled properly — no `!` operator unless justified
- [ ] Async all the way — no `.Result` or `.Wait()` calls
- [ ] No magic strings — use constants classes or `nameof(EnumValue)`
- [ ] No hardcoded URLs, status codes, or API values — everything via constants
- [ ] Records with primary constructors for commands, queries, handlers, DTOs
- [ ] `SaveChangesAsync` uses `request.AppName ?? nameof(XxxCommand)` pattern
- [ ] Catch blocks that only log are intentional for enrichment steps — partial success preferred

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
- [ ] Points NEVER stored — computed on the fly from `Outcome` + `scoring_rules` table
- [ ] Use `ScoreCastDateTime.Now` instead of `DateTime.UtcNow`

## Endpoints & API
- [ ] POST endpoints: all data from `[Body]` request model — no route `{parameter}` placeholders
- [ ] GET endpoints: route parameters as method parameters
- [ ] POST Refit methods: `[Body] RequestType` signature
- [ ] `UserId` auto-populated by `KeycloakUserPreprocessor` — never set manually

## Blazor / Frontend
- [ ] No `@code` blocks in `.razor` files — always use code-behind `.razor.cs` files
- [ ] All API client calls wrapped in `Loading.While`
- [ ] ViewModels in `ScoreCast.Web/ViewModels/`
- [ ] Pages inject Refit interfaces directly (no controller layer)

## External APIs
- [ ] Pulse is primary data source for Premier League
- [ ] Football-data.org is fallback for non-PL or when Pulse fails
- [ ] FPL API used for Pulse ID mappings
- [ ] New Pulse endpoints added to `PulseApi.Routes` constants
- [ ] Pulse response models use `[property: JsonPropertyName]` for case-sensitive deserialization

## Security
- [ ] No secrets in code or config files
- [ ] Input validation on all endpoints
- [ ] `AllowAnonymous` only for: health check, landing page, login, sign up, about, contact us
