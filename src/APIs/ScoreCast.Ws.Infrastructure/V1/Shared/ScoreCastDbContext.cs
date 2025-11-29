using Microsoft.EntityFrameworkCore;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Infrastructure.V1.Shared;

public sealed class ScoreCastDbContext(DbContextOptions<ScoreCastDbContext> options)
    : DbContext(options), IScoreCastDbContext
{
    public DbSet<UserMaster> UserMasters => Set<UserMaster>();
    public DbSet<RoleMaster> RoleMasters => Set<RoleMaster>();
    public DbSet<PageMaster> PageMasters => Set<PageMaster>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePage> RolePages => Set<RolePage>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Competition> Competitions => Set<Competition>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Season> Seasons => Set<Season>();
    public DbSet<SeasonTeam> SeasonTeams => Set<SeasonTeam>();
    public DbSet<Gameweek> Gameweeks => Set<Gameweek>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<Prediction> Predictions => Set<Prediction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("scorecast");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScoreCastDbContext).Assembly);
    }
}
