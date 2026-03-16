using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Types;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetTeamMatchesQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetTeamMatchesQuery, ScoreCastResponse<TeamMatchesResult>>
{
    public async Task<ScoreCastResponse<TeamMatchesResult>> ExecuteAsync(GetTeamMatchesQuery query, CancellationToken ct)
    {
        var seasonIds = query.SeasonId.HasValue
            ? [query.SeasonId.Value]
            : await DbContext.SeasonTeams
                .AsNoTracking()
                .Where(st => st.TeamId == query.TeamId && st.Season.IsCurrent)
                .Select(st => st.SeasonId)
                .ToListAsync(ct);

        var matches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => (m.HomeTeamId == query.TeamId || m.AwayTeamId == query.TeamId)
                && seasonIds.Contains(m.Gameweek.SeasonId))
            .OrderBy(m => m.KickoffTime)
            .Select(m => new
            {
                m.Id, m.KickoffTime, Status = m.Status.ToString(),
                m.HomeTeamId,
                HomeName = m.HomeTeam.Name, HomeLogo = m.HomeTeam.LogoUrl, HomeShort = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                m.AwayTeamId,
                AwayName = m.AwayTeam.Name, AwayLogo = m.AwayTeam.LogoUrl, AwayShort = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                m.HomeScore, m.AwayScore, m.Venue, m.Referee, m.Minute,
                CompName = m.Gameweek.Season.Competition.Name,
                CompLogo = m.Gameweek.Season.Competition.LogoUrl
            })
            .ToListAsync(ct);

        var matchIds = matches.Select(m => m.Id).ToList();

        var events = await DbContext.MatchEvents
            .AsNoTracking()
            .Where(e => matchIds.Contains(e.MatchId))
            .Select(e => new { e.MatchId, e.Player.Name, EventType = e.EventType.ToString(), e.Value, e.PlayerId, e.Minute })
            .ToListAsync(ct);

        var playerIds = events.Select(e => e.PlayerId).Distinct().ToList();
        var playerTeamMap = await DbContext.TeamPlayers
            .AsNoTracking()
            .Where(tp => seasonIds.Contains(tp.SeasonId) && playerIds.Contains(tp.PlayerId))
            .Select(tp => new { tp.PlayerId, tp.TeamId })
            .ToDictionaryAsync(tp => tp.PlayerId, tp => tp.TeamId, ct);

        var eventsByMatch = events.GroupBy(e => e.MatchId).ToDictionary(g => g.Key, g => g.ToList());

        var result = matches.Select(m => new TeamMatchDetail(
            m.Id, m.KickoffTime.HasValue ? new ScoreCastDateTime(m.KickoffTime.Value) : null,
            m.Status, m.HomeTeamId, m.HomeName, m.HomeLogo, m.HomeShort,
            m.AwayTeamId, m.AwayName, m.AwayLogo, m.AwayShort,
            m.HomeScore, m.AwayScore, m.Venue, m.Referee, m.Minute,
            m.CompName, m.CompLogo,
            eventsByMatch.GetValueOrDefault(m.Id, []).Select(e =>
            {
                var isHome = playerTeamMap.GetValueOrDefault(e.PlayerId) == m.HomeTeamId;
                return new MatchEventDetail(e.Name, e.EventType, e.Value, isHome, e.Minute);
            }).ToList()
        )).ToList();

        return ScoreCastResponse<TeamMatchesResult>.Ok(new TeamMatchesResult(result));
    }
}
