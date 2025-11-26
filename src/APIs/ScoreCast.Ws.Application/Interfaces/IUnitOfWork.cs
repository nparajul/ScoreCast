namespace ScoreCast.Ws.Application.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(string menuName, CancellationToken ct);
}
