using Microsoft.EntityFrameworkCore;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Infrastructure.V1.Shared;

public sealed class ScoreCastDbContext(DbContextOptions<ScoreCastDbContext> options)
    : DbContext(options), IScoreCastDbContext
{
    public DbSet<UserMaster> UserMasters => Set<UserMaster>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("scorecast");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScoreCastDbContext).Assembly);
    }
}
