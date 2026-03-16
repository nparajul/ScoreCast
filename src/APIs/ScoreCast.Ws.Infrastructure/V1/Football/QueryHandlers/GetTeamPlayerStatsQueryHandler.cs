using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetTeamPlayerStatsQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetTeamPlayerStatsQuery, ScoreCastResponse<PlayerStatsResult>>
{
    public async Task<ScoreCastResponse<PlayerStatsResult>> ExecuteAsync(GetTeamPlayerStatsQuery query, CancellationToken ct)
    {
        // Get all current season IDs for this team, or the specific one
        var seasonIds = query.SeasonId.HasValue
            ? [query.SeasonId.Value]
            : await DbContext.SeasonTeams
                .AsNoTracking()
                .Where(st => st.TeamId == query.TeamId && st.Season.IsCurrent)
                .Select(st => st.SeasonId)
                .ToListAsync(ct);

        // Get player IDs belonging to this team
        var teamPlayerIds = await DbContext.TeamPlayers
            .AsNoTracking()
            .Where(tp => tp.TeamId == query.TeamId && seasonIds.Contains(tp.SeasonId))
            .Select(tp => tp.PlayerId)
            .Distinct()
            .ToListAsync(ct);

        // Run the standard stats query for each season and merge, filtering to team players
        var allRows = new List<PlayerStatRow>();
        foreach (var seasonId in seasonIds)
        {
            var result = await new GetPlayerStatsQuery(seasonId).ExecuteAsync(ct);
            if (result is { Success: true, Data: not null })
                allRows.AddRange(result.Data.Rows.Where(r => teamPlayerIds.Contains(r.PlayerId)));
        }

        // Merge rows across seasons (same player may appear in multiple)
        var merged = allRows
            .GroupBy(r => r.PlayerId)
            .Select(g => new PlayerStatRow(
                g.Key, g.First().PlayerName, g.First().PlayerImageUrl,
                g.First().TeamName, g.First().TeamLogo, g.First().Position,
                g.Sum(r => r.Goals), g.Sum(r => r.PenaltyGoals), g.Sum(r => r.OwnGoals),
                g.Sum(r => r.Assists), g.Sum(r => r.YellowCards), g.Sum(r => r.RedCards),
                g.Sum(r => r.CleanSheets)))
            .ToList();

        return ScoreCastResponse<PlayerStatsResult>.Ok(new PlayerStatsResult(merged));
    }
}
