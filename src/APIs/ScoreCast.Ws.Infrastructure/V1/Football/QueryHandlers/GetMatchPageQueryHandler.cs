using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetMatchPageQueryHandler(
    IScoreCastDbContext DbContext,
    IHttpClientFactory HttpClientFactory) : IQueryHandler<GetMatchPageQuery, ScoreCastResponse<MatchPageResult>>
{
    public async Task<ScoreCastResponse<MatchPageResult>> ExecuteAsync(GetMatchPageQuery query, CancellationToken ct)
    {
        var match = await DbContext.Matches.AsNoTracking()
            .Where(m => m.Id == query.MatchId)
            .Select(m => new
            {
                m.Id, m.KickoffTime, m.Status, m.Minute, m.HomeScore, m.AwayScore, m.Venue, m.Referee,
                m.HomeTeamId, m.AwayTeamId,
                HomeTeamName = m.HomeTeam.Name, HomeTeamLogo = m.HomeTeam.LogoUrl,
                HomeTeamShortName = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                AwayTeamName = m.AwayTeam.Name, AwayTeamLogo = m.AwayTeam.LogoUrl,
                AwayTeamShortName = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                CompetitionName = m.Gameweek.Season.Competition.Name,
                CompetitionLogo = m.Gameweek.Season.Competition.LogoUrl,
                SeasonId = m.Gameweek.SeasonId
            })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return ScoreCastResponse<MatchPageResult>.Error("Match not found.");

        // Events from DB
        var events = await DbContext.MatchEvents.AsNoTracking()
            .Where(e => e.MatchId == query.MatchId)
            .Select(e => new { e.Player.Name, EventType = e.EventType.ToString(), e.Minute, e.PlayerId })
            .ToListAsync(ct);

        var playerIds = events.Select(e => e.PlayerId).Distinct().ToList();
        var playerTeamMap = await DbContext.TeamPlayers
            .Where(tp => tp.SeasonId == match.SeasonId && playerIds.Contains(tp.PlayerId))
            .Select(tp => new { tp.PlayerId, tp.TeamId })
            .ToDictionaryAsync(tp => tp.PlayerId, tp => tp.TeamId, ct);

        // Build assist map: group goal events with assist events by minute
        var assistMap = events
            .Where(e => e.EventType == EventTypes.Assist)
            .ToDictionary(e => $"{e.Minute}", e => e.Name);

        var matchEvents = events
            .Where(e => e.EventType != EventTypes.Assist)
            .Select(e =>
            {
                var isHome = playerTeamMap.GetValueOrDefault(e.PlayerId) == match.HomeTeamId;
                var isGoal = e.EventType is EventTypes.Goal or EventTypes.PenaltyGoal;
                string? assistName = isGoal ? assistMap.GetValueOrDefault($"{e.Minute}") : null;
                return new MatchPageEvent(e.EventType, e.Name, assistName, e.Minute, isHome, ParseMinute(e.Minute));
            })
            .ToList();

        // Try Pulse for lineups/formation
        string? homeFormation = null, awayFormation = null;
        var homeLineup = new List<MatchPageLineupPlayer>();
        var homeSubs = new List<MatchPageLineupPlayer>();
        var awayLineup = new List<MatchPageLineupPlayer>();
        var awaySubs = new List<MatchPageLineupPlayer>();
        int? htHome = null, htAway = null;

        var pulseMapping = await DbContext.ExternalMappings.AsNoTracking()
            .Where(m => m.EntityId == query.MatchId && m.EntityType == EntityType.Match
                        && (m.Source == ExternalSource.Pulse || m.Source == ExternalSource.Fpl))
            .Select(m => m.ExternalCode)
            .FirstOrDefaultAsync(ct);

        if (pulseMapping is not null)
        {
            try
            {
                var client = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.PulseClient));
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                var pulse = await client.GetFromJsonAsync<PulseFixtureResponse>(
                    string.Format(PulseApi.Routes.Fixture, pulseMapping), cts.Token);

                if (pulse is not null)
                {
                    htHome = pulse.HalfTimeScore?.HomeScore;
                    htAway = pulse.HalfTimeScore?.AwayScore;

                    // Build Pulse player ID → our player ID map
                    var pulsePlayerIds = (pulse.TeamLists ?? [])
                        .SelectMany(tl => (tl.Lineup ?? []).Concat(tl.Substitutes ?? []))
                        .Select(p => p.Id.ToString())
                        .ToList();

                    var pulsePlayerMap = pulsePlayerIds.Count > 0
                        ? await DbContext.ExternalMappings.AsNoTracking()
                            .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Player
                                        && pulsePlayerIds.Contains(m.ExternalCode))
                            .ToDictionaryAsync(m => m.ExternalCode, m => m.EntityId, ct)
                        : [];

                    var ourPlayerIds = pulsePlayerMap.Values.ToList();
                    var playerPhotos = ourPlayerIds.Count > 0
                        ? await DbContext.Players.AsNoTracking()
                            .Where(p => ourPlayerIds.Contains(p.Id))
                            .ToDictionaryAsync(p => p.Id, p => p.PhotoUrl, ct)
                        : [];

                    // Event icons per player
                    var playerIcons = events
                        .GroupBy(e => e.PlayerId)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.EventType).ToList());

                    // Pulse team → our team mapping
                    var pulseTeamMap = await DbContext.ExternalMappings.AsNoTracking()
                        .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Team)
                        .ToDictionaryAsync(m => m.ExternalCode, m => m.EntityId, ct);

                    foreach (var tl in pulse.TeamLists ?? [])
                    {
                        var teamId = tl.Team is not null && pulseTeamMap.TryGetValue(tl.Team.Id.ToString(), out var tid) ? tid : 0L;
                        var isHome = teamId == match.HomeTeamId;

                        var lineup = (tl.Lineup ?? []).Select(p => MapPlayer(p, pulsePlayerMap, playerPhotos, playerIcons)).ToList();
                        var subs = (tl.Substitutes ?? []).Select(p => MapPlayer(p, pulsePlayerMap, playerPhotos, playerIcons)).ToList();

                        if (isHome)
                        {
                            homeFormation = tl.Formation?.Label;
                            homeLineup = lineup;
                            homeSubs = subs;
                        }
                        else
                        {
                            awayFormation = tl.Formation?.Label;
                            awayLineup = lineup;
                            awaySubs = subs;
                        }
                    }
                }
            }
            catch { /* Pulse unavailable — show page without lineups */ }
        }

        return ScoreCastResponse<MatchPageResult>.Ok(new MatchPageResult(
            match.Id, match.KickoffTime, match.Status.ToString(), match.Minute,
            match.HomeTeamId, match.HomeTeamName, match.HomeTeamLogo, match.HomeTeamShortName,
            match.AwayTeamId, match.AwayTeamName, match.AwayTeamLogo, match.AwayTeamShortName,
            match.HomeScore, match.AwayScore, match.Venue, match.Referee,
            htHome, htAway, match.CompetitionName, match.CompetitionLogo,
            homeFormation, awayFormation,
            homeLineup, homeSubs, awayLineup, awaySubs,
            matchEvents));
    }

    private static MatchPageLineupPlayer MapPlayer(
        PulsePlayer p,
        Dictionary<string, long> pulsePlayerMap,
        Dictionary<long, string?> playerPhotos,
        Dictionary<long, List<string>> playerIcons)
    {
        var ourId = pulsePlayerMap.GetValueOrDefault(p.Id.ToString());
        var photo = ourId > 0 ? playerPhotos.GetValueOrDefault(ourId) : null;
        var icons = ourId > 0 ? playerIcons.GetValueOrDefault(ourId, []) : [];
        return new MatchPageLineupPlayer(
            ourId, p.Name?.Display ?? "Unknown", photo,
            p.MatchShirtNumber, p.MatchPosition, p.Captain ?? false, icons);
    }

    private static double ParseMinute(string? minute)
    {
        if (minute is null) return 999;
        var clean = minute.Replace("'", "").Replace(" ", "");
        var parts = clean.Split('+');
        if (double.TryParse(parts[0], out var main))
            return parts.Length > 1 && double.TryParse(parts[1], out var added) ? main + added * 0.01 : main;
        return 999;
    }
}
