using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetPlayerStatsQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetPlayerStatsQuery, ScoreCastResponse<PlayerStatsResult>>
{
    public async Task<ScoreCastResponse<PlayerStatsResult>> ExecuteAsync(GetPlayerStatsQuery query, CancellationToken ct)
    {
        var seasonMatchIds = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == query.SeasonId && m.Status == MatchStatus.Finished)
            .Select(m => m.Id)
            .ToListAsync(ct);

        var events = await DbContext.MatchEvents
            .AsNoTracking()
            .Where(e => seasonMatchIds.Contains(e.MatchId))
            .Select(e => new { e.PlayerId, e.EventType, e.Value })
            .ToListAsync(ct);

        var playerIds = events.Select(e => e.PlayerId).Distinct().ToList();

        var playerTeams = await DbContext.TeamPlayers
            .AsNoTracking()
            .Where(tp => tp.SeasonId == query.SeasonId && playerIds.Contains(tp.PlayerId))
            .Select(tp => new { tp.PlayerId, tp.Team.Name, tp.Team.LogoUrl })
            .ToDictionaryAsync(tp => tp.PlayerId, tp => (tp.Name, tp.LogoUrl), ct);

        var playerNames = await DbContext.Players
            .AsNoTracking()
            .Where(p => playerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.PhotoUrl })
            .ToDictionaryAsync(p => p.Id, p => (p.Name, p.PhotoUrl), ct);

        var rows = events
            .GroupBy(e => e.PlayerId)
            .Select(g =>
            {
                var team = playerTeams.GetValueOrDefault(g.Key);
                var player = playerNames.GetValueOrDefault(g.Key);
                return new PlayerStatRow(
                    g.Key,
                    player.Name ?? SharedConstants.Unknown,
                    player.PhotoUrl,
                    team.Name,
                    team.LogoUrl,
                    g.Where(e => e.EventType == MatchEventType.Goal).Sum(e => e.Value),
                    g.Where(e => e.EventType == MatchEventType.PenaltyGoal).Sum(e => e.Value),
                    g.Where(e => e.EventType == MatchEventType.OwnGoal).Sum(e => e.Value),
                    g.Where(e => e.EventType == MatchEventType.Assist).Sum(e => e.Value),
                    g.Where(e => e.EventType == MatchEventType.YellowCard).Sum(e => e.Value),
                    g.Where(e => e.EventType == MatchEventType.RedCard).Sum(e => e.Value));
            })
            .Where(r => r.Goals + r.PenaltyGoals + r.Assists + r.YellowCards + r.RedCards > 0)
            .ToList();

        return ScoreCastResponse<PlayerStatsResult>.Ok(new PlayerStatsResult(rows));
    }
}
