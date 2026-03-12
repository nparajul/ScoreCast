using Microsoft.EntityFrameworkCore;
using ScoreCast.Ws.Application.Interfaces;

namespace ScoreCast.Ws.Infrastructure.Data;

public sealed class ScoreCastDbContext(DbContextOptions<ScoreCastDbContext> options)
    : DbContext(options), IScoreCastDbContext, IUnitOfWork
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScoreCastDbContext).Assembly);
    }

    async Task<int> IUnitOfWork.SaveChangesAsync(string menuName, string userRole, CancellationToken ct)
    {
        // TODO: use menuName/userRole for audit logging
        return await base.SaveChangesAsync(ct);
    }
}
