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
    public DbSet<Stage> Stages => Set<Stage>();
    public DbSet<MatchGroup> MatchGroups => Set<MatchGroup>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<PredictionScoringRule> PredictionScoringRules => Set<PredictionScoringRule>();
    public DbSet<PredictionLeague> PredictionLeagues => Set<PredictionLeague>();
    public DbSet<PredictionLeagueMember> PredictionLeagueMembers => Set<PredictionLeagueMember>();
    public DbSet<Prediction> Predictions => Set<Prediction>();
    public DbSet<RiskPlay> RiskPlays => Set<RiskPlay>();
    public DbSet<Player> Players => Set<Player>();
    public DbSet<Coach> Coaches => Set<Coach>();
    public DbSet<TeamPlayer> TeamPlayers => Set<TeamPlayer>();
    public DbSet<CompetitionZone> CompetitionZones => Set<CompetitionZone>();
    public DbSet<MatchEvent> MatchEvents => Set<MatchEvent>();
    public DbSet<MatchLineup> MatchLineups => Set<MatchLineup>();
    public DbSet<ExternalMapping> ExternalMappings => Set<ExternalMapping>();
    public DbSet<AppConfig> AppConfigs => Set<AppConfig>();
    public DbSet<MatchInsightCache> MatchInsightCaches => Set<MatchInsightCache>();
    public DbSet<MatchHighlight> MatchHighlights => Set<MatchHighlight>();
    public DbSet<UserSeason> UserSeasons => Set<UserSeason>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SharedConstants.DefaultSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ScoreCastDbContext).Assembly);
    }
}
