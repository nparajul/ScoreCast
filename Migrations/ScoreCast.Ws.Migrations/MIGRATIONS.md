# EF Core Migration Commands

All commands run from the solution root.

## Add a New Migration

```bash
dotnet ef migrations add <MigrationName> \
  --project Migrations/ScoreCast.Ws.Migrations \
  --startup-project src/APIs/ScoreCast.Ws \
  --context ScoreCastDbContext
```

## Generate SQL Script

```bash
dotnet ef migrations script \
  --project Migrations/ScoreCast.Ws.Migrations \
  --startup-project src/APIs/ScoreCast.Ws \
  --context ScoreCastDbContext \
  --output Migrations/ScoreCast.Ws.Migrations/Scripts/<MigrationName>.sql
```

## Remove Last Migration (if not yet applied)

```bash
dotnet ef migrations remove \
  --project Migrations/ScoreCast.Ws.Migrations \
  --startup-project src/APIs/ScoreCast.Ws \
  --context ScoreCastDbContext
```

## List All Migrations

```bash
dotnet ef migrations list \
  --project Migrations/ScoreCast.Ws.Migrations \
  --startup-project src/APIs/ScoreCast.Ws \
  --context ScoreCastDbContext
```
