using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetGameweekMatchesQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetGameweekMatchesQuery, ScoreCastResponse<GameweekMatchesResult>>
{
    public async Task<ScoreCastResponse<GameweekMatchesResult>> ExecuteAsync(GetGameweekMatchesQuery query, CancellationToken ct)
    {
        var totalGameweeks = await DbContext.Gameweeks
            .CountAsync(g => g.SeasonId == query.SeasonId, ct);

        if (totalGameweeks == 0)
            return ScoreCastResponse<GameweekMatchesResult>.Error("No gameweeks found for this season.");

        var currentGameweek = await DbContext.Gameweeks
            .Where(g => g.SeasonId == query.SeasonId && g.Matches.Any(m => m.Status == MatchStatus.Live))
            .Select(g => (int?)g.Number)
            .FirstOrDefaultAsync(ct)
          ?? await DbContext.Gameweeks
            .Where(g => g.SeasonId == query.SeasonId && g.Matches.Any(m => m.Status != MatchStatus.Finished))
            .OrderBy(g => g.Number)
            .Select(g => (int?)g.Number)
            .FirstOrDefaultAsync(ct)
          ?? totalGameweeks;

        // If no gameweek specified, use the current one
        var gameweek = query.GameweekNumber.HasValue
            ? await DbContext.Gameweeks.FirstOrDefaultAsync(g => g.SeasonId == query.SeasonId && g.Number == query.GameweekNumber, ct)
            : await DbContext.Gameweeks.FirstOrDefaultAsync(g => g.SeasonId == query.SeasonId && g.Number == currentGameweek, ct);

        if (gameweek is null)
            return ScoreCastResponse<GameweekMatchesResult>.Error("Gameweek not found.");

        var matches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.GameweekId == gameweek.Id)
            .OrderBy(m => m.KickoffTime)
            .Select(m => new
            {
                m.Id, m.KickoffTime, m.Status, m.HomeScore, m.AwayScore, m.Venue, m.Referee, m.Minute,
                m.HomeTeamId, m.AwayTeamId,
                HomeTeamName = m.HomeTeam.Name, HomeTeamLogo = m.HomeTeam.LogoUrl,
                HomeTeamShortName = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                AwayTeamName = m.AwayTeam.Name, AwayTeamLogo = m.AwayTeam.LogoUrl,
                AwayTeamShortName = m.AwayTeam.ShortName ?? m.AwayTeam.Name
            })
            .ToListAsync(ct);

        var matchIds = matches.Select(m => m.Id).ToList();
        var events = await DbContext.MatchEvents
            .AsNoTracking()
            .Where(e => matchIds.Contains(e.MatchId))
            .Select(e => new { e.MatchId, e.Player.Name, EventType = e.EventType.ToString(), e.Value, e.PlayerId, e.Minute })
            .ToListAsync(ct);

        // Determine player team via TeamPlayer
        var playerIds = events.Select(e => e.PlayerId).Distinct().ToList();
        var playerTeamMap = await DbContext.TeamPlayers
            .Where(tp => tp.SeasonId == query.SeasonId && playerIds.Contains(tp.PlayerId))
            .Select(tp => new { tp.PlayerId, tp.TeamId })
            .ToDictionaryAsync(tp => tp.PlayerId, tp => tp.TeamId, ct);

        var eventsByMatch = events.GroupBy(e => e.MatchId).ToDictionary(g => g.Key, g => g.ToList());
        var matchTeams = matches.ToDictionary(m => m.Id, m => m.HomeTeamId);

        var result = matches.Select(m => new MatchDetail(
            m.Id, m.KickoffTime, m.Status.ToString(),
            m.HomeTeamId, m.HomeTeamName, m.HomeTeamLogo, m.HomeTeamShortName,
            m.AwayTeamId, m.AwayTeamName, m.AwayTeamLogo, m.AwayTeamShortName,
            m.HomeScore, m.AwayScore, m.Venue, m.Referee, m.Minute,
            eventsByMatch.GetValueOrDefault(m.Id, []).OrderBy(e => ParseMinute(e.Minute)).Select(e =>
            {
                var playerTeamId = playerTeamMap.GetValueOrDefault(e.PlayerId);
                var isHome = playerTeamId == m.HomeTeamId;
                return new MatchEventDetail(e.Name, e.EventType, e.Value, isHome, e.Minute);
            }).ToList()
        )).ToList();

        return ScoreCastResponse<GameweekMatchesResult>.Ok(
            new GameweekMatchesResult(gameweek.Id, gameweek.Number, gameweek.StartDate, gameweek.EndDate, totalGameweeks, currentGameweek, result));
    }

    private static double ParseMinute(string? minute)
    {
        if (minute is null) return 999;
        // "45+2'" → 45.2, "90'" → 90, "90 +4'" → 90.4
        var clean = minute.Replace("'", "").Replace(" ", "");
        var parts = clean.Split('+');
        if (double.TryParse(parts[0], out var main))
            return parts.Length > 1 && double.TryParse(parts[1], out var added) ? main + added * 0.01 : main;
        return 999;
    }
}
