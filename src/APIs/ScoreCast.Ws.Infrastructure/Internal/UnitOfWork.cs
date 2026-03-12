using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.Internal;

internal sealed class UnitOfWork(ScoreCastDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(string menuName, CancellationToken ct)
        => dbContext.SaveChangesAsync(ct);
}
