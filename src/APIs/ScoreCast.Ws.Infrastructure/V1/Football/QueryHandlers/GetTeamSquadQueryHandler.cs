using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetTeamSquadQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetTeamSquadQuery, ScoreCastResponse<TeamSquadResult>>
{
    public async Task<ScoreCastResponse<TeamSquadResult>> ExecuteAsync(GetTeamSquadQuery query, CancellationToken ct)
    {
        var seasonId = query.SeasonId ?? await DbContext.SeasonTeams
            .AsNoTracking()
            .Where(st => st.TeamId == query.TeamId && st.Season.IsCurrent)
            .Select(st => st.SeasonId)
            .FirstOrDefaultAsync(ct);

        var players = await DbContext.TeamPlayers
            .AsNoTracking()
            .Where(tp => tp.TeamId == query.TeamId && tp.SeasonId == seasonId)
            .OrderBy(tp => tp.Player.Position).ThenBy(tp => tp.Player.Name)
            .Select(tp => new SquadPlayer(
                tp.PlayerId, tp.Player.Name, tp.Player.Position,
                tp.Player.PhotoUrl, tp.Player.Nationality, tp.Player.DateOfBirth))
            .ToListAsync(ct);

        return ScoreCastResponse<TeamSquadResult>.Ok(new TeamSquadResult(players));
    }
}
