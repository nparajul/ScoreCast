using Microsoft.EntityFrameworkCore;
using ScoreCast.Shared.Constants;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Domain.V1.Entities;
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
    public DbSet<Player> Players => Set<Player>();
    public DbSet<TeamPlayer> TeamPlayers => Set<TeamPlayer>();
    public DbSet<CompetitionZone> CompetitionZones => Set<CompetitionZone>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<ExternalMapping> ExternalMappings => Set<ExternalMapping>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SharedConstants.DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScoreCastDbContext).Assembly);
    }
}
