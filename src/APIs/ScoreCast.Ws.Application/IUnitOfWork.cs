using Microsoft.EntityFrameworkCore.Storage;

namespace ScoreCast.Ws.Application;

public interface IUnitOfWork
{
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct);
    Task<int> SaveChangesAsync(string menuName, CancellationToken ct);
}
