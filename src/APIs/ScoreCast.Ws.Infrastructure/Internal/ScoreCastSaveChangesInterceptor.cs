using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ScoreCast.Shared.Types;
using ScoreCast.Ws.Domain.V1;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.Internal;

public sealed class ScoreCastSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    private static ScoreCastDateTime CurrentDate => ScoreCastDateTime.Now;

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new())
    {
        if (eventData.Context is not ScoreCastDbContext dbContext)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext is null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var userId = httpContext.User?.Identity?.Name ?? "system";

        foreach (var entry in dbContext.ChangeTracker.Entries<IAuditable>())
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = CurrentDate;
                    entry.Entity.CreatedBy = userId;
                    entry.Entity.ModifiedDate = CurrentDate;
                    entry.Entity.ModifiedBy = userId;
                    entry.Entity.IsDeleted = false;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedDate = CurrentDate;
                    entry.Entity.ModifiedBy = userId;
                    break;
            }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
