# Coding Standards

## General
- Target .NET 10, C# latest
- `TreatWarningsAsErrors` is enabled — no warnings allowed
- File-scoped namespaces everywhere
- Nullable reference types enabled

## Naming
- Private fields: `_camelCase`
- Public properties: `PascalCase`
- Interfaces: `I` prefix (e.g., `IMatchRepository`)
- Async methods: `Async` suffix

## Architecture Rules
- **Domain** must have zero external dependencies (no NuGet packages, no project references)
- **Application** defines commands, queries, and interfaces only — no implementations
- **Infrastructure** implements all handlers and data access
- **Endpoints** are thin — delegate to commands/queries, no business logic
- Entity configurations (EF Fluent API) go in `Infrastructure/Data/Configurations/`

## FastEndpoints Conventions
- Use `Send.OkAsync()` for sending responses — NOT `SendAsync()` or `SendOkAsync()`
- Endpoints inherit `EndpointWithoutRequest<TResponse>` or `Endpoint<TRequest, TResponse>`
- Commands are `public`, handlers are `internal sealed`
- `AllowAnonymous()` is ONLY permitted on public-facing endpoints: health check, landing page, login, sign up, about, contact us. All other endpoints must require authentication.

## Code Style
- **Records everywhere** for commands, queries, handlers, DTOs, and value objects
- Use primary constructors for dependency injection via record syntax
- Commands: `public record AbcCommand(string Name, int Value) : ICommand<AbcResponse>;`
- Handlers: `internal sealed record AbcCommandHandler(IScoreCastDbContext DbContext, IUnitOfWork UnitOfWork) : ICommandHandler<AbcCommand, AbcResponse>`
- Commands are `public`, handlers are `internal sealed`
- No `var` for non-obvious types
- Use expression-bodied members for single-line methods/properties
- Keep methods short — if it needs a comment explaining what a block does, extract a method

## Dependencies
- Central Package Management via `Directory.Packages.props` — never specify versions in individual `.csproj` files
- FastEndpoints for endpoints and CQRS — no MediatR
- EF Core for data access
