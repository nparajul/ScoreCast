using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.MasterData.Commands;
using ScoreCast.Ws.Domain.V1.Entities;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.CommandHandlers;

internal sealed record EnhanceLiveMatchesCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory,
    ILogger<EnhanceLiveMatchesCommandHandler> Logger) : ICommandHandler<EnhanceLiveMatchesCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(EnhanceLiveMatchesCommand command, CancellationToken ct)
    {
        // 1. Get current season
        var currentSeason = await DbContext.Seasons
            .FirstOrDefaultAsync(s => s.Competition.Code == CompetitionCodes.PremierLeague && s.IsCurrent, ct);
        if (currentSeason is null)
            return ScoreCastResponse.Error("No current season found.");

        // 2. FPL — ensure Pulse ID mappings exist for started matches
        var fplClient = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FplClient));
        try
        {
            var fplFixtures = await fplClient.GetFromJsonAsync<List<FplFixture>>(FplApi.Routes.Fixtures, ct);
            if (fplFixtures is { Count: > 0 })
                await EnsurePulseMappingsAsync(fplFixtures, currentSeason, ct);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "FPL fetch failed");
        }

        await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(EnhanceLiveMatchesCommand), ct);

        // 3. Pulse (primary for PL) — get live matches, scores, clock, events
        var pulseClient = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.PulseClient));
        var liveMatchIds = await EnrichFromPulseAsync(pulseClient, currentSeason, ct);

        // 4. Football-data.org (secondary) — update non-live matches only (final scores, status transitions)
        await EnrichFromFootballDataAsync(currentSeason, liveMatchIds, ct);

        await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(EnhanceLiveMatchesCommand), ct);
        return ScoreCastResponse.Ok($"Enhanced {liveMatchIds.Count} live matches.");
    }

    private async Task EnsurePulseMappingsAsync(List<FplFixture> fplFixtures, Season season, CancellationToken ct)
    {
        var existingMappedIds = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Fpl && m.EntityType == EntityType.Match)
            .Select(m => m.EntityId)
            .ToListAsync(ct);
        var mappedSet = existingMappedIds.ToHashSet();

        // Build FPL team code → our team ID
        var fplTeamMappings = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Fpl && m.EntityType == EntityType.Team)
            .ToDictionaryAsync(m => m.ExternalCode, m => m.EntityId, ct);

        var bootstrap = await HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FplClient))
            .GetFromJsonAsync<FplBootstrapResponse>(FplApi.Routes.BootstrapStatic, ct);
        if (bootstrap is null) return;

        var fplTeamIdToCode = bootstrap.Teams.ToDictionary(t => t.Id, t => t.Code);

        var matchLookup = await DbContext.Matches
            .Where(m => m.Gameweek.SeasonId == season.Id)
            .Select(m => new { m.Id, m.HomeTeamId, m.AwayTeamId })
            .ToListAsync(ct);
        var matchByTeams = matchLookup.ToDictionary(m => (m.HomeTeamId, m.AwayTeamId), m => m.Id);

        foreach (var fixture in fplFixtures.Where(f => f.Started == true && f.PulseId.HasValue))
        {
            var homeCode = fplTeamIdToCode.GetValueOrDefault(fixture.TeamH);
            var awayCode = fplTeamIdToCode.GetValueOrDefault(fixture.TeamA);
            if (!fplTeamMappings.TryGetValue(homeCode.ToString(), out var homeTeamId) ||
                !fplTeamMappings.TryGetValue(awayCode.ToString(), out var awayTeamId))
                continue;
            if (!matchByTeams.TryGetValue((homeTeamId, awayTeamId), out var matchId)) continue;
            if (!mappedSet.Add(matchId)) continue;

            DbContext.ExternalMappings.Add(new ExternalMapping
            {
                EntityType = EntityType.Match,
                EntityId = matchId,
                Source = ExternalSource.Fpl,
                ExternalCode = fixture.PulseId!.Value.ToString()
            });
        }
    }

    private async Task<List<long>> EnrichFromPulseAsync(HttpClient pulseClient, Season season, CancellationToken ct)
    {
        var liveMatchIds = new List<long>();

        // Only fetch non-finished matches — no need to re-fetch completed ones
        var candidateMatches = await DbContext.Matches
            .Where(m => m.Gameweek.SeasonId == season.Id && m.Status != MatchStatus.Finished)
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (candidateMatches.Count == 0) return liveMatchIds;

        var pulseMappings = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Fpl && m.EntityType == EntityType.Match
                        && candidateMatches.Contains(m.EntityId))
            .ToDictionaryAsync(m => m.EntityId, m => int.Parse(m.ExternalCode), ct);

        if (pulseMappings.Count == 0) return liveMatchIds;

        var candidateIds = pulseMappings.Keys.ToList();

        var dbMatches = await DbContext.Matches
            .Where(m => candidateIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, ct);

        var matchTeams = await DbContext.Matches
            .Where(m => candidateIds.Contains(m.Id))
            .Select(m => new { m.Id, m.HomeTeamId, m.AwayTeamId })
            .ToDictionaryAsync(m => m.Id, ct);

        var pulseTeamMap = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Team)
            .ToDictionaryAsync(m => m.ExternalCode, m => m.EntityId, ct);

        // Fetch all candidate fixtures from Pulse
        using var semaphore = new SemaphoreSlim(5);
        var fetchTasks = pulseMappings.Select(async kv =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(10));
                var data = await pulseClient.GetFromJsonAsync<PulseFixtureResponse>(
                    string.Format(PulseApi.Routes.Fixture, kv.Value), cts.Token);
                return (MatchId: kv.Key, Data: data);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Pulse fixture fetch failed for {PulseId}: {Error}", kv.Value, ex.Message);
                return (MatchId: kv.Key, Data: (PulseFixtureResponse?)null);
            }
            finally { semaphore.Release(); }
        });

        var results = await Task.WhenAll(fetchTasks);

        // Update status, scores, minute from Pulse for all fetched fixtures
        foreach (var (matchId, pulseData) in results)
        {
            if (pulseData is null || !dbMatches.TryGetValue(matchId, out var dbMatch)) continue;

            var pulseStatus = MapPulseStatus(pulseData.Status);
            dbMatch.Status = pulseStatus;

            if (pulseStatus == MatchStatus.Live)
            {
                liveMatchIds.Add(matchId);
                dbMatch.Minute = pulseData.Phase switch
                {
                    PulseApi.Phase.HalfTime => PulseApi.DisplayLabels.HalfTime,
                    _ when pulseData.Clock is not null => pulseData.Clock.Label.Replace("'00", "'"),
                    _ => dbMatch.Minute
                };
            }
            else if (pulseStatus == MatchStatus.Finished)
            {
                dbMatch.Minute = PulseApi.DisplayLabels.FullTime;
            }

            if (pulseData.Teams is { Count: 2 } && matchTeams.TryGetValue(matchId, out var mt))
            {
                var pulseHome = pulseData.Teams.FirstOrDefault(t => t.Team is not null && pulseTeamMap.TryGetValue(t.Team.Id.ToString(), out var tid) && tid == mt.HomeTeamId);
                var pulseAway = pulseData.Teams.FirstOrDefault(t => t.Team is not null && pulseTeamMap.TryGetValue(t.Team.Id.ToString(), out var tid) && tid == mt.AwayTeamId);
                if (pulseHome is not null) dbMatch.HomeScore = pulseHome.Score;
                if (pulseAway is not null) dbMatch.AwayScore = pulseAway.Score;
            }
        }

        // Events enrichment — only for live matches
        if (liveMatchIds.Count == 0) return liveMatchIds;

        var existingEvents = await DbContext.MatchEvents
            .Where(e => liveMatchIds.Contains(e.MatchId))
            .Select(e => $"{e.MatchId}|{e.PlayerId}|{e.EventType}|{e.Minute}")
            .ToListAsync(ct);
        var existingEventKeys = existingEvents.ToHashSet();

        var pulsePlayerMap = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Player)
            .ToDictionaryAsync(m => m.ExternalCode, m => m.EntityId, ct);

        var validPlayerIds = await DbContext.TeamPlayers
            .Where(tp => tp.SeasonId == season.Id)
            .Select(tp => tp.PlayerId)
            .ToListAsync(ct);
        var validSet = validPlayerIds.ToHashSet();

        var eventCount = 0;

        foreach (var (matchId, pulseData) in results)
        {
            if (pulseData?.Events is null || !liveMatchIds.Contains(matchId)) continue;
            if (!matchTeams.TryGetValue(matchId, out _)) continue;

            foreach (var pe in pulseData.Events)
            {
                if (pe.PersonId is null) continue;
                var eventType = MapPulseEvent(pe.Type, pe.Description);
                if (eventType is null) continue;

                var personKey = pe.PersonId.Value.ToString();
                if (!pulsePlayerMap.TryGetValue(personKey, out var playerId)) continue;
                if (!validSet.Contains(playerId)) continue;

                var minute = pe.Clock?.Label?.Replace("'00", "'");
                var key = $"{matchId}|{playerId}|{eventType.Value}|{minute}";
                if (!existingEventKeys.Add(key)) continue;

                DbContext.MatchEvents.Add(new MatchEvent
                {
                    MatchId = matchId, PlayerId = playerId,
                    EventType = eventType.Value, Value = 1, Minute = minute
                });
                eventCount++;

                if (pe.AssistId is not null && eventType is MatchEventType.Goal or MatchEventType.PenaltyGoal)
                {
                    var assistKey = pe.AssistId.Value.ToString();
                    if (pulsePlayerMap.TryGetValue(assistKey, out var assistPlayerId) && validSet.Contains(assistPlayerId))
                    {
                        var aKey = $"{matchId}|{assistPlayerId}|{MatchEventType.Assist}|{minute}";
                        if (existingEventKeys.Add(aKey))
                        {
                            DbContext.MatchEvents.Add(new MatchEvent
                            {
                                MatchId = matchId, PlayerId = assistPlayerId,
                                EventType = MatchEventType.Assist, Value = 1, Minute = minute
                            });
                            eventCount++;
                        }
                    }
                }
            }
        }

        Logger.LogInformation("Enhanced {Count} live matches, added {Events} events", liveMatchIds.Count, eventCount);
        return liveMatchIds;
    }

    private static MatchStatus MapPulseStatus(string? status) => status switch
    {
        PulseApi.Status.Live => MatchStatus.Live,
        PulseApi.Status.Complete => MatchStatus.Finished,
        _ => MatchStatus.Scheduled
    };

    private async Task EnrichFromFootballDataAsync(Season season, List<long> pulseHandledIds, CancellationToken ct)
    {
        try
        {
            var fdClient = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FootballDataClient));
            var fdResponse = await fdClient.GetFromJsonAsync<FootballDataMatchesResponse>(
                string.Format(FootballDataApi.Routes.Matches, CompetitionCodes.PremierLeague, season.StartDate.Year), ct);

            if (fdResponse?.Matches is not { Count: > 0 }) return;

            var pulseHandledSet = pulseHandledIds.ToHashSet();
            var dbMatches = await DbContext.Matches
                .Where(m => m.Gameweek.SeasonId == season.Id && m.ExternalId != null && !pulseHandledSet.Contains(m.Id))
                .ToDictionaryAsync(m => m.ExternalId!, ct);

            foreach (var apiMatch in fdResponse.Matches)
            {
                if (!dbMatches.TryGetValue(apiMatch.Id.ToString(), out var match)) continue;

                var status = MapStatus(apiMatch.Status);
                match.HomeScore = apiMatch.Score.FullTime?.Home ?? match.HomeScore;
                match.AwayScore = apiMatch.Score.FullTime?.Away ?? match.AwayScore;
                match.Status = status;
                match.Minute = FormatMinute(apiMatch) ?? match.Minute;
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Football-data.org fallback fetch failed");
        }
    }

    private static MatchStatus MapStatus(string apiStatus) => apiStatus switch
    {
        FootballDataApi.Status.Finished => MatchStatus.Finished,
        FootballDataApi.Status.InPlay or FootballDataApi.Status.Paused or FootballDataApi.Status.Live => MatchStatus.Live,
        FootballDataApi.Status.Postponed or FootballDataApi.Status.Suspended => MatchStatus.Postponed,
        FootballDataApi.Status.Cancelled => MatchStatus.Cancelled,
        _ => MatchStatus.Scheduled
    };

    private static string? FormatMinute(FootballDataMatch apiMatch) => apiMatch.Status switch
    {
        FootballDataApi.Status.Finished => PulseApi.DisplayLabels.FullTime,
        FootballDataApi.Status.Paused => PulseApi.DisplayLabels.HalfTime,
        FootballDataApi.Status.InPlay when apiMatch.Minute.HasValue =>
            apiMatch.InjuryTime > 0 ? $"{apiMatch.Minute}+{apiMatch.InjuryTime}'" : $"{apiMatch.Minute}'",
        _ => null
    };

    private static MatchEventType? MapPulseEvent(string type, string? desc) => (type, desc) switch
    {
        ("G", "G") => MatchEventType.Goal,
        ("G", "P") => MatchEventType.PenaltyGoal,
        ("P", "P") => MatchEventType.PenaltyGoal,
        ("O", "O") => MatchEventType.OwnGoal,
        ("B", "Y") => MatchEventType.YellowCard,
        ("B", "R") => MatchEventType.RedCard,
        ("B", "YR") => MatchEventType.RedCard,
        ("MP", "MP") => MatchEventType.PenaltyMissed,
        ("SP", "SP") => MatchEventType.PenaltySaved,
        ("S", "ON") => MatchEventType.SubIn,
        ("S", "OFF") => MatchEventType.SubOut,
        _ => null
    };
}
