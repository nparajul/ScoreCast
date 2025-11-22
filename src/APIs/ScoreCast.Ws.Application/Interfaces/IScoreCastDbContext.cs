using Microsoft.EntityFrameworkCore;

namespace ScoreCast.Ws.Application.Interfaces;

public interface IScoreCastDbContext
{
    DbSet<T> Set<T>() where T : class;
}
