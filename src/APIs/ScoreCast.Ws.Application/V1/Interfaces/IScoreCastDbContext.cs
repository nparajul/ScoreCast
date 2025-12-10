using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ScoreCast.Ws.Domain.V1.Entities;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Application.V1.Interfaces;

public interface IScoreCastDbContext
{
    DatabaseFacade Database { get; }
    DbSet<UserMaster> UserMasters { get; }
    DbSet<RoleMaster> RoleMasters { get; }
    DbSet<PageMaster> PageMasters { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RolePage> RolePages { get; }
    DbSet<Country> Countries { get; }
    DbSet<Competition> Competitions { get; }
    DbSet<Team> Teams { get; }
    DbSet<Season> Seasons { get; }
    DbSet<SeasonTeam> SeasonTeams { get; }
    DbSet<Gameweek> Gameweeks { get; }
    DbSet<Stage> Stages { get; }
    DbSet<MatchGroup> MatchGroups { get; }
    DbSet<Match> Matches { get; }
    DbSet<PredictionScoringRule> PredictionScoringRules { get; }
    DbSet<PredictionLeague> PredictionLeagues { get; }
    DbSet<PredictionLeagueMember> PredictionLeagueMembers { get; }
    DbSet<Prediction> Predictions { get; }
    DbSet<Player> Players { get; }
    DbSet<TeamPlayer> TeamPlayers { get; }
    DbSet<CompetitionZone> CompetitionZones { get; }
    DbSet<MatchEvent> MatchEvents { get; }
    DbSet<MatchLineup> MatchLineups { get; }
    DbSet<ExternalMapping> ExternalMappings { get; }
    DbSet<AppConfig> AppConfigs { get; }
}
