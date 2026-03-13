using Microsoft.EntityFrameworkCore;
using ScoreCast.Ws.Domain.V1.Entities.League;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Application.Interfaces;

public interface IScoreCastDbContext
{
    DbSet<UserMaster> UserMasters { get; }
    DbSet<CountryMaster> CountryMasters { get; }
    DbSet<LeagueMaster> LeagueMasters { get; }
    DbSet<TeamMaster> TeamMasters { get; }
}
