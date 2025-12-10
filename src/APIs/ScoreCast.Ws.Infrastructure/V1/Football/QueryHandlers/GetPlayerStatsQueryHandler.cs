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
    IScoreCastDbContext DbContext) : IQueryHandler<GetPlayerStatsQuery, ScoreCastResponse<PlayerStatsResult>>
{
    public async Task<ScoreCastResponse<PlayerStatsResult>> ExecuteAsync(GetPlayerStatsQuery query, CancellationToken ct)
    {
        var seasonMatches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == query.SeasonId && m.Status == MatchStatus.Finished)
            .Select(m => new { m.Id, m.HomeTeamId, m.AwayTeamId, m.HomeScore, m.AwayScore })
            .ToListAsync(ct);

        var seasonMatchIds = seasonMatches.Select(m => m.Id).ToList();

        var events = await DbContext.MatchEvents
            .AsNoTracking()
            .Where(e => seasonMatchIds.Contains(e.MatchId))
            .Select(e => new { e.MatchId, e.PlayerId, e.EventType, e.Value, e.Minute })
            .ToListAsync(ct);

        var playerIds = events.Select(e => e.PlayerId).Distinct().ToList();

        var playerTeams = await DbContext.TeamPlayers
            .AsNoTracking()
            .Where(tp => tp.SeasonId == query.SeasonId && playerIds.Contains(tp.PlayerId))
            .Select(tp => new { tp.PlayerId, tp.TeamId, tp.Team.Name, tp.Team.LogoUrl })
            .ToDictionaryAsync(tp => tp.PlayerId, tp => (tp.TeamId, tp.Name, tp.LogoUrl), ct);

        var playerInfo = await DbContext.Players
            .AsNoTracking()
            .Where(p => playerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.PhotoUrl, p.Position })
            .ToDictionaryAsync(p => p.Id, p => (p.Name, p.PhotoUrl, p.Position), ct);

        // Clean sheets: GKs who started matches where their team conceded 0
        var cleanSheetMatches = seasonMatches
            .SelectMany(m => new[]
            {
                (MatchId: m.Id, TeamId: m.HomeTeamId, Conceded: m.AwayScore ?? 0),
                (MatchId: m.Id, TeamId: m.AwayTeamId, Conceded: m.HomeScore ?? 0)
            })
            .Where(x => x.Conceded == 0)
            .Select(x => (x.MatchId, x.TeamId))
            .ToHashSet();

        var gkStarts = await DbContext.MatchLineups
            .AsNoTracking()
            .Where(l => seasonMatchIds.Contains(l.MatchId) && l.IsStarter && l.Player.Position == PlayerPositions.Goalkeeper)
            .Select(l => new { l.MatchId, l.PlayerId })
            .ToListAsync(ct);

        // Ensure GK starters are included in playerIds/playerInfo/playerTeams
        var gkIds = gkStarts.Select(g => g.PlayerId).Distinct().ToList();
        var missingGkIds = gkIds.Except(playerIds).ToList();
        if (missingGkIds.Count > 0)
        {
            var extraTeams = await DbContext.TeamPlayers
                .AsNoTracking()
                .Where(tp => tp.SeasonId == query.SeasonId && missingGkIds.Contains(tp.PlayerId))
                .Select(tp => new { tp.PlayerId, tp.TeamId, tp.Team.Name, tp.Team.LogoUrl })
                .ToListAsync(ct);
            foreach (var t in extraTeams)
                playerTeams.TryAdd(t.PlayerId, (t.TeamId, t.Name, t.LogoUrl));

            var extraPlayers = await DbContext.Players
                .AsNoTracking()
                .Where(p => missingGkIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name, p.PhotoUrl, p.Position })
                .ToListAsync(ct);
            foreach (var p in extraPlayers)
                playerInfo.TryAdd(p.Id, (p.Name, p.PhotoUrl, p.Position));
        }

        // GK sub events to determine clean sheet eligibility
        var gkSubEvents = events
            .Where(e => gkIds.Contains(e.PlayerId) && e.EventType is MatchEventType.SubOut or MatchEventType.SubIn)
            .ToLookup(e => (e.MatchId, e.PlayerId));

        var cleanSheetCounts = new Dictionary<long, int>();
        foreach (var start in gkStarts)
        {
            if (!playerTeams.TryGetValue(start.PlayerId, out var team)) continue;
            if (!cleanSheetMatches.Contains((start.MatchId, team.TeamId))) continue;

            // If subbed off before 60', no clean sheet
            var subOut = gkSubEvents[(start.MatchId, start.PlayerId)]
                .FirstOrDefault(e => e.EventType == MatchEventType.SubOut);
            if (subOut is not null && ParseMinute(subOut.Minute) < 60) continue;

            cleanSheetCounts[start.PlayerId] = cleanSheetCounts.GetValueOrDefault(start.PlayerId) + 1;
        }

        // GKs subbed on before 30': clean sheet if team conceded 0 after they came on
        var gkSubOns = await DbContext.MatchLineups
            .AsNoTracking()
            .Where(l => seasonMatchIds.Contains(l.MatchId) && !l.IsStarter && l.Player.Position == PlayerPositions.Goalkeeper)
            .Select(l => new { l.MatchId, l.PlayerId })
            .ToListAsync(ct);

        var subOnGkIds = gkSubOns.Select(g => g.PlayerId).Distinct().Except(gkIds).ToList();
        if (subOnGkIds.Count > 0)
        {
            var extraIds = subOnGkIds.Except(playerIds).ToList();
            if (extraIds.Count > 0)
            {
                var extraTeams = await DbContext.TeamPlayers
                    .AsNoTracking()
                    .Where(tp => tp.SeasonId == query.SeasonId && extraIds.Contains(tp.PlayerId))
                    .Select(tp => new { tp.PlayerId, tp.TeamId, tp.Team.Name, tp.Team.LogoUrl })
                    .ToListAsync(ct);
                foreach (var t in extraTeams)
                    playerTeams.TryAdd(t.PlayerId, (t.TeamId, t.Name, t.LogoUrl));

                var extraPlayers = await DbContext.Players
                    .AsNoTracking()
                    .Where(p => extraIds.Contains(p.Id))
                    .Select(p => new { p.Id, p.Name, p.PhotoUrl, p.Position })
                    .ToListAsync(ct);
                foreach (var p in extraPlayers)
                    playerInfo.TryAdd(p.Id, (p.Name, p.PhotoUrl, p.Position));
            }
        }

        // Goal events by match for sub-on GK check
        var goalsByMatch = events
            .Where(e => e.EventType is MatchEventType.Goal or MatchEventType.PenaltyGoal or MatchEventType.OwnGoal)
            .ToLookup(e => e.MatchId);

        foreach (var subOn in gkSubOns)
        {
            if (!playerTeams.TryGetValue(subOn.PlayerId, out var team)) continue;
            var subInEvent = events.FirstOrDefault(e => e.MatchId == subOn.MatchId && e.PlayerId == subOn.PlayerId && e.EventType == MatchEventType.SubIn);
            if (subInEvent is null) continue;
            var subMinute = ParseMinute(subInEvent.Minute);
            if (subMinute >= 30) continue;

            // Check if opponent scored any goals after sub-on minute
            var match = seasonMatches.First(m => m.Id == subOn.MatchId);
            var opponentTeamId = team.TeamId == match.HomeTeamId ? match.AwayTeamId : match.HomeTeamId;
            var opponentGoalsAfter = goalsByMatch[subOn.MatchId]
                .Count(g =>
                {
                    var scorerTeam = playerTeams.GetValueOrDefault(g.PlayerId);
                    var isOwnGoal = g.EventType == MatchEventType.OwnGoal;
                    var goalForOpponent = isOwnGoal ? scorerTeam.TeamId == team.TeamId : scorerTeam.TeamId == opponentTeamId;
                    return goalForOpponent && ParseMinute(g.Minute) >= subMinute;
                });

            if (opponentGoalsAfter == 0)
                cleanSheetCounts[subOn.PlayerId] = cleanSheetCounts.GetValueOrDefault(subOn.PlayerId) + 1;
        }

        var rows = events
            .GroupBy(e => e.PlayerId)
            .Select(g =>
            {
                var team = playerTeams.GetValueOrDefault(g.Key);
                var player = playerInfo.GetValueOrDefault(g.Key);
                return new PlayerStatRow(
                    g.Key,
                    player.Name ?? SharedConstants.Unknown,
                    player.PhotoUrl,
                    team.Name,
                    team.LogoUrl,
                    player.Position,
                    g.Where(e => e.EventType == MatchEventType.Goal).Sum(e => e.Value),
                    g.Where(e => e.EventType == MatchEventType.PenaltyGoal).Sum(e => e.Value),
                    g.Where(e => e.EventType == MatchEventType.OwnGoal).Sum(e => e.Value),
                    g.Where(e => e.EventType == MatchEventType.Assist).Sum(e => e.Value),
                    g.Where(e => e.EventType == MatchEventType.YellowCard).Sum(e => e.Value),
                    g.Where(e => e.EventType == MatchEventType.RedCard).Sum(e => e.Value),
                    cleanSheetCounts.GetValueOrDefault(g.Key));
            })
            .Where(r => r.Goals + r.PenaltyGoals + r.Assists + r.YellowCards + r.RedCards + r.CleanSheets > 0)
            .ToList();

        // Add GKs who have clean sheets but no events
        var eventPlayerIds = rows.Select(r => r.PlayerId).ToHashSet();
        foreach (var (gkId, cs) in cleanSheetCounts)
        {
            if (eventPlayerIds.Contains(gkId)) continue;
            var team = playerTeams.GetValueOrDefault(gkId);
            var player = playerInfo.GetValueOrDefault(gkId);
            rows.Add(new PlayerStatRow(gkId, player.Name ?? SharedConstants.Unknown, player.PhotoUrl,
                team.Name, team.LogoUrl, player.Position, 0, 0, 0, 0, 0, 0, cs));
        }

        return ScoreCastResponse<PlayerStatsResult>.Ok(new PlayerStatsResult(rows));
    }

    private static int ParseMinute(string? minute)
    {
        if (string.IsNullOrWhiteSpace(minute)) return 0;
        var clean = minute.Replace("'", "").Trim();
        var plusIndex = clean.IndexOf('+');
        if (plusIndex >= 0) clean = clean[..plusIndex].Trim();
        return int.TryParse(clean, out var m) ? m : 0;
    }
}
