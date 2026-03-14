using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetTeamsQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetTeamsQuery, ScoreCastResponse<List<TeamResult>>>
{
    public async Task<ScoreCastResponse<List<TeamResult>>> ExecuteAsync(GetTeamsQuery query, CancellationToken ct)
    {
        var teams = await DbContext.SeasonTeams
            .AsNoTracking()
            .Where(st => st.Season.Competition.Name == query.CompetitionName && st.Season.IsCurrent && st.Team.IsActive)
            .OrderBy(st => st.Team.Name)
            .Select(st => new TeamResult(st.Team.Id, st.Team.Name, st.Team.ShortName, st.Team.LogoUrl))
            .ToListAsync(ct);

        return ScoreCastResponse<List<TeamResult>>.Ok(teams);
    }
}
