using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Domain.V1;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.Internal;

internal class UnitOfWork(ScoreCastDbContext dbContext) : IUnitOfWork, IDisposable, IAsyncDisposable
{
    private bool _disposed;

    public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct) =>
        await dbContext.Database.BeginTransactionAsync(ct);

    public Task<int> SaveChangesAsync(string appName, CancellationToken ct)
    {
        UpdateAuditableEntities(appName);
        return dbContext.SaveChangesAsync(ct);
    }

    private void UpdateAuditableEntities(string appName)
    {
        var entries = dbContext.ChangeTracker.Entries<IAuditable>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Property(a => a.CreatedByApp).CurrentValue = appName;
                entry.Property(a=>a.ModifiedByApp).CurrentValue = appName;
                entry.Property(a=>a.IsDeleted).CurrentValue = false;
            }

            if (entry.State != EntityState.Modified) continue;

            entry.Property(a => a.ModifiedByApp).CurrentValue = appName;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        dbContext.Dispose();
        _disposed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        await dbContext.DisposeAsync();
        _disposed = true;
    }
}
