using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Application.Interfaces;

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
    DbSet<Match> Matches { get; }
    DbSet<Prediction> Predictions { get; }
}
