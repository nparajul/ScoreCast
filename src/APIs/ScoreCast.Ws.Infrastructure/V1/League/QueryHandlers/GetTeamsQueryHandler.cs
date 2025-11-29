using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.League;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Application.V1.League.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.League.QueryHandlers;

internal sealed record GetTeamsQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetTeamsQuery, ScoreCastResponse<List<TeamResult>>>
{
    public async Task<ScoreCastResponse<List<TeamResult>>> ExecuteAsync(GetTeamsQuery query, CancellationToken ct)
    {
        var teams = await DbContext.TeamMasters
            .AsNoTracking()
            .Where(t => t.IsActive && t.League.Name == query.LeagueName)
            .OrderBy(t => t.Name)
            .Select(t => new TeamResult(t.Id, t.Name, t.ShortName, t.LogoUrl))
            .ToListAsync(ct);

        return ScoreCastResponse<List<TeamResult>>.Ok(teams);
    }
}
