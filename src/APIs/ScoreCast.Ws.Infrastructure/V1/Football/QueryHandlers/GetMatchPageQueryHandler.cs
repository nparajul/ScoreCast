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
                HomeCoach = m.HomeCoach != null ? m.HomeCoach.Name : null,
                HomeCoachPhoto = m.HomeCoach != null ? m.HomeCoach.PhotoUrl : null,
                AwayTeamName = m.AwayTeam.Name, AwayTeamLogo = m.AwayTeam.LogoUrl,
                AwayTeamShortName = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                AwayCoach = m.AwayCoach != null ? m.AwayCoach.Name : null,
                AwayCoachPhoto = m.AwayCoach != null ? m.AwayCoach.PhotoUrl : null,
                CompetitionName = m.Gameweek.Season.Competition.Name,
                CompetitionLogo = m.Gameweek.Season.Competition.LogoUrl,
                CompetitionCode = m.Gameweek.Season.Competition.Code,
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

        // Build assist map
        var assistMap = events
            .Where(e => e.EventType == EventTypes.Assist)
            .ToDictionary(e => $"{e.Minute}", e => e.Name);

        // Build sub pairs: SubIn → find matching SubOut at same minute/team
        var subOuts = events.Where(e => e.EventType == EventTypes.SubOut).ToList();

        // Build all events (excluding raw assists and SubOut — SubOut merged into SubIn)
        var allEvents = events
            .Where(e => e.EventType is not EventTypes.Assist and not EventTypes.SubOut)
            .Select(e =>
            {
                var isHome = playerTeamMap.GetValueOrDefault(e.PlayerId) == match.HomeTeamId;
                if (e.EventType == EventTypes.OwnGoal) isHome = !isHome;
                var isGoal = e.EventType is EventTypes.Goal or EventTypes.PenaltyGoal or EventTypes.OwnGoal;
                string? assistName = e.EventType is EventTypes.Goal or EventTypes.PenaltyGoal
                    ? assistMap.GetValueOrDefault($"{e.Minute}") : null;
                string? playerOff = null;
                if (e.EventType == EventTypes.SubIn)
                {
                    var so = subOuts.FirstOrDefault(s => s.Minute == e.Minute
                        && playerTeamMap.GetValueOrDefault(s.PlayerId) == playerTeamMap.GetValueOrDefault(e.PlayerId));
                    if (so is not null) { playerOff = so.Name; subOuts.Remove(so); }
                }
                return new MatchPageEvent(e.EventType, e.Name, assistName, e.Minute, isHome,
                    ParseMinute(e.Minute), playerOff, null);
            })
            .OrderBy(e => e.SortKey)
            .ToList();

        // Compute running score for goal events
        int rHome = 0, rAway = 0;
        var matchEvents = allEvents.Select(e =>
        {
            if (e.EventType is EventTypes.Goal or EventTypes.PenaltyGoal or EventTypes.OwnGoal)
            {
                if (e.IsHome) rHome++; else rAway++;
                return e with { RunningScore = $"{rHome} - {rAway}" };
            }
            return e;
        }).ToList();

        // Try Pulse for lineups/formation
        string? homeFormation = null, awayFormation = null;
        var homeLineup = new List<MatchPageLineupPlayer>();
        var homeSubs = new List<MatchPageLineupPlayer>();
        var awayLineup = new List<MatchPageLineupPlayer>();
        var awaySubs = new List<MatchPageLineupPlayer>();
        int? htHome = null, htAway = null;
        string? phase = null;
        long? firstHalfStartMillis = null;
        long? secondHalfStartMillis = null;

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
                    phase = pulse.Phase;

                    var ps1 = pulse.Events?.FirstOrDefault(e => e.Type == "PS" && e.Phase == "1");
                    firstHalfStartMillis = ps1?.Time?.Millis;

                    var ps2 = pulse.Events?.FirstOrDefault(e => e.Type == "PS" && e.Phase == "2");
                    if (ps2?.Time?.Millis is not null)
                        secondHalfStartMillis = ps2.Time.Millis.Value;
                    else
                    {
                        var pe1 = pulse.Events?.FirstOrDefault(e => e.Type == "PE" && e.Phase == "1");
                        if (pe1?.Time?.Millis is not null)
                            secondHalfStartMillis = pe1.Time.Millis.Value + (long)TimeSpan.FromMinutes(15).TotalMilliseconds;
                    }

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

                    // Sub minutes: SubOut minute for starters, SubIn minute for subs
                    var subMinutes = events
                        .Where(e => e.EventType is EventTypes.SubOut or EventTypes.SubIn)
                        .ToDictionary(e => e.PlayerId, e => (string?)e.Minute);

                    // teamLists[0] = home, teamLists[1] = away (Pulse convention)
                    var teamLists = pulse.TeamLists ?? [];
                    for (var i = 0; i < teamLists.Count && i < 2; i++)
                    {
                        var tl = teamLists[i];
                        var isHome = i == 0;

                        // Reorder lineup by formation player IDs (GK → DEF → MID → FWD)
                        var formationOrder = (tl.Formation?.Players ?? []).SelectMany(row => row).ToList();
                        var lineupById = (tl.Lineup ?? []).ToDictionary(p => p.Id);
                        var orderedLineup = formationOrder
                            .Where(lineupById.ContainsKey)
                            .Select(id => MapPlayer(lineupById[id], pulsePlayerMap, playerPhotos, playerIcons, subMinutes))
                            .ToList();
                        foreach (var p in tl.Lineup ?? [])
                            if (!formationOrder.Contains(p.Id))
                                orderedLineup.Add(MapPlayer(p, pulsePlayerMap, playerPhotos, playerIcons, subMinutes));

                        var subs = (tl.Substitutes ?? []).Select(p => MapPlayer(p, pulsePlayerMap, playerPhotos, playerIcons, subMinutes)).ToList();

                        if (isHome)
                        {
                            homeFormation = tl.Formation?.Label;
                            homeLineup = orderedLineup;
                            homeSubs = subs;
                        }
                        else
                        {
                            awayFormation = tl.Formation?.Label;
                            awayLineup = orderedLineup;
                            awaySubs = subs;
                        }
                    }
                }
            }
            catch { /* Pulse unavailable — show page without lineups */ }
        }

        return ScoreCastResponse<MatchPageResult>.Ok(new MatchPageResult(
            match.Id, match.KickoffTime, match.Status.ToString(), match.Minute,
            firstHalfStartMillis, phase, secondHalfStartMillis,
            match.HomeTeamId, match.HomeTeamName, match.HomeTeamLogo, match.HomeTeamShortName,
            match.AwayTeamId, match.AwayTeamName, match.AwayTeamLogo, match.AwayTeamShortName,
            match.HomeScore, match.AwayScore, match.Venue, match.Referee,
            htHome, htAway, match.CompetitionName, match.CompetitionLogo, match.CompetitionCode, match.SeasonId,
            homeFormation, awayFormation,
            match.HomeCoach, match.AwayCoach,
            match.HomeCoachPhoto, match.AwayCoachPhoto,
            homeLineup, homeSubs, awayLineup, awaySubs,
            matchEvents));
    }

    private static MatchPageLineupPlayer MapPlayer(
        PulsePlayer p,
        Dictionary<string, long> pulsePlayerMap,
        Dictionary<long, string?> playerPhotos,
        Dictionary<long, List<string>> playerIcons,
        Dictionary<long, string?> subMinutes)
    {
        var ourId = pulsePlayerMap.GetValueOrDefault(p.Id.ToString());
        var photo = ourId > 0 ? playerPhotos.GetValueOrDefault(ourId) : null;
        var icons = ourId > 0 ? playerIcons.GetValueOrDefault(ourId, []) : [];
        var subMin = ourId > 0 ? subMinutes.GetValueOrDefault(ourId) : null;
        return new MatchPageLineupPlayer(
            ourId, p.Name?.Display ?? "Unknown", photo,
            p.MatchShirtNumber, p.MatchPosition, p.Captain ?? false, icons, subMin);
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
