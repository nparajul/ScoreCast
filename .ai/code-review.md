# Code Review Checklist

## Architecture
- [ ] No business logic in Endpoints — they should only map requests to commands/queries
- [ ] Domain entities have no dependencies on Infrastructure or Application
- [ ] New NuGet packages added to `Directory.Packages.props`, not individual csproj files
- [ ] No circular project references

## Code Quality
- [ ] No compiler warnings (TreatWarningsAsErrors is on)
- [ ] Nullable reference types handled properly — no `!` operator unless justified
- [ ] Async all the way — no `.Result` or `.Wait()` calls
- [ ] No magic strings — use constants or enums

## Naming & Style
- [ ] File-scoped namespaces used
- [ ] Private fields use `_camelCase`
- [ ] Async methods suffixed with `Async`

## Data Access
- [ ] Entity configurations use Fluent API in `Infrastructure/Data/Configurations/`
- [ ] No EF attributes/annotations on Domain entities
- [ ] Queries use `AsNoTracking()` where appropriate

## Security
- [ ] No secrets in code or config files
- [ ] Input validation on all endpoints
