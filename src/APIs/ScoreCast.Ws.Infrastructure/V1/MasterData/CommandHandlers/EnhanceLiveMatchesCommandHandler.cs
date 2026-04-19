using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Exceptions;
using ScoreCast.Shared.Types;
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
        try
        {
            return await ExecuteCoreAsync(command, ct);
        }
        catch (ScoreCastException ex)
        {
            return ScoreCastResponse.Error(ex.Message);
        }
    }

    private async Task<ScoreCastResponse> ExecuteCoreAsync(EnhanceLiveMatchesCommand command, CancellationToken ct)
    {
        // 1. Get target seasons
        List<Season> currentSeasons;
        if (command.Request.SeasonId.HasValue)
        {
            var season = await DbContext.Seasons
                .Include(s => s.Competition)
                .FirstOrDefaultAsync(s => s.Id == command.Request.SeasonId.Value, ct);
            if (season is null)
                return ScoreCastResponse.NotFound($"Season {command.Request.SeasonId} not found.");
            currentSeasons = [season];
        }
        else
        {
            currentSeasons = await DbContext.Seasons
                .Include(s => s.Competition)
                .Where(s => s.IsCurrent)
                .ToListAsync(ct);
        }

        if (currentSeasons.Count == 0)
            return ScoreCastResponse.Error("No current seasons found.");

        var totalEnhanced = 0;
        var warnings = new List<string>();

        foreach (var season in currentSeasons)
        {
            var isPremierLeague = season.Competition.Code == CompetitionCodes.PremierLeague;

            if (isPremierLeague)
            {
                // FPL — ensure Pulse ID mappings exist for started matches
                var fplClient = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FplClient));
                try
                {
                    var fplFixtures = await fplClient.GetFromJsonAsync<List<FplFixture>>(FplApi.Routes.Fixtures, ct);
                    if (fplFixtures is { Count: > 0 })
                        await EnsurePulseMappingsAsync(fplFixtures, season, ct);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex, "FPL fetch failed");
                }

                await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(EnhanceLiveMatchesCommand), ct);

                // Pulse — primary for PL
                var pulseClient = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.PulseClient));
                var (liveMatchIds, updatedCount) = await EnrichFromPulseAsync(pulseClient, season, ct);
                totalEnhanced += updatedCount;
            }
        }

        // Football-data.org — single bulk call for all non-PL competitions
        var nonPlSeasons = currentSeasons.Where(s => s.Competition.Code != CompetitionCodes.PremierLeague).ToList();
        if (nonPlSeasons.Count > 0)
        {
            var (count, warning) = await EnrichFromFootballDataBulkAsync(nonPlSeasons, ct);
            totalEnhanced += count;
            if (warning is not null) warnings.Add(warning);
        }

        await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(EnhanceLiveMatchesCommand), ct);
        var msg = $"Enhanced {totalEnhanced} matches across {currentSeasons.Count} competitions.";
        if (warnings.Count > 0) msg += $" Skipped: {string.Join("; ", warnings)}";
        return ScoreCastResponse.Ok(msg);
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

    private async Task<(List<long> LiveMatchIds, int UpdatedCount)> EnrichFromPulseAsync(HttpClient pulseClient, Season season, CancellationToken ct)
    {
        var liveMatchIds = new List<long>();
        var newlyFinishedMatchIds = new List<long>();
        var updatedCount = 0;

        // Only fetch non-finished matches — no need to re-fetch completed ones
        var candidateMatches = await DbContext.Matches
            .Where(m => m.Gameweek.SeasonId == season.Id && m.Status != MatchStatus.Finished)
            .Select(m => m.Id)
            .ToListAsync(ct);

        if (candidateMatches.Count == 0) return (liveMatchIds, updatedCount);

        var pulseMappings = await DbContext.ExternalMappings
            .Where(m => (m.Source == ExternalSource.Fpl || m.Source == ExternalSource.Pulse)
                        && m.EntityType == EntityType.Match
                        && candidateMatches.Contains(m.EntityId))
            .GroupBy(m => m.EntityId)
            .ToDictionaryAsync(g => g.Key, g => int.Parse(g.First().ExternalCode), ct);

        if (pulseMappings.Count == 0) return (liveMatchIds, updatedCount);

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

            updatedCount++;
            var pulseStatus = MapPulseStatus(pulseData.Status, pulseData.Phase);
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
                newlyFinishedMatchIds.Add(matchId);
                dbMatch.Minute = PulseApi.DisplayLabels.FullTime;
            }

            if (pulseData.Teams is { Count: 2 } && matchTeams.TryGetValue(matchId, out var mt))
            {
                var pulseHome = pulseData.Teams.FirstOrDefault(t => t.Team is not null && pulseTeamMap.TryGetValue(t.Team.Id.ToString(), out var tid) && tid == mt.HomeTeamId);
                var pulseAway = pulseData.Teams.FirstOrDefault(t => t.Team is not null && pulseTeamMap.TryGetValue(t.Team.Id.ToString(), out var tid) && tid == mt.AwayTeamId);
                if (pulseHome is not null) dbMatch.HomeScore = (int?)pulseHome.Score;
                if (pulseAway is not null) dbMatch.AwayScore = (int?)pulseAway.Score;
            }
        }

        // Events enrichment — for live and newly finished matches
        var eventMatchIds = liveMatchIds.Concat(newlyFinishedMatchIds).ToList();
        if (eventMatchIds.Count == 0) return (liveMatchIds, updatedCount);

        var existingEvents = await DbContext.MatchEvents
            .Where(e => eventMatchIds.Contains(e.MatchId))
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
            if (pulseData?.Events is null || !eventMatchIds.Contains(matchId)) continue;
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

        Logger.LogInformation("Enhanced {Count} live matches ({Finished} newly finished), added {Events} events", liveMatchIds.Count, newlyFinishedMatchIds.Count, eventCount);
        return (liveMatchIds, updatedCount);
    }

    private static MatchStatus MapPulseStatus(string? status, string? phase = null) => status switch
    {
        PulseApi.Status.Live => MatchStatus.Live,
        PulseApi.Status.Complete => MatchStatus.Finished,
        PulseApi.Status.Postponed => MatchStatus.Postponed,
        _ when phase == PulseApi.Phase.Postponed => MatchStatus.Postponed,
        _ => MatchStatus.Scheduled
    };

    private static MatchEventType? MapPulseEvent(string type, string? desc) => (type, desc) switch
    {
        ("G", "G") => MatchEventType.Goal,
        ("G", "P") => MatchEventType.PenaltyGoal,
        ("P", "P") => MatchEventType.PenaltyGoal,
        ("O", "O") => MatchEventType.OwnGoal,
        ("B", "Y") => MatchEventType.YellowCard,
        ("B", "R") => MatchEventType.RedCard,
        ("B", "YR") => MatchEventType.SecondYellow,
        ("MP", "MP") => MatchEventType.PenaltyMissed,
        ("SP", "SP") => MatchEventType.PenaltySaved,
        ("S", "ON") => MatchEventType.SubIn,
        ("S", "OFF") => MatchEventType.SubOut,
        _ => null
    };

    private async Task<(int Count, string? Warning)> EnrichFromFootballDataBulkAsync(List<Season> seasons, CancellationToken ct)
    {
        var fdClient = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FootballDataClient));
        var today = ScoreCastDateTime.Now.Date;
        var dateFrom = today.ToString("yyyy-MM-dd");
        var dateTo = today.AddDays(1).ToString("yyyy-MM-dd");

        FootballDataMatchesResponse? fdResponse;
        try
        {
            fdResponse = await fdClient.GetFromJsonAsync<FootballDataMatchesResponse>(
                string.Format(FootballDataApi.Routes.MatchesByDate, dateFrom, dateTo), ct);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.TooManyRequests)
        {
            var reason = ex.StatusCode == System.Net.HttpStatusCode.Forbidden ? "Restricted" : "Rate limited";
            return (0, $"Bulk matches: {reason}");
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Football-data.org bulk matches API failed");
            return (0, $"Football-data.org: {ex.Message}");
        }

        if (fdResponse?.Matches is not { Count: > 0 }) return (0, null);

        // Build lookup: competition code → season
        var seasonByCode = seasons.ToDictionary(s => s.Competition.Code, s => s);

        // Load all external-id-mapped matches across these seasons
        var seasonIds = seasons.Select(s => s.Id).ToList();
        var dbMatches = await DbContext.Matches
            .Where(m => seasonIds.Contains(m.Gameweek.SeasonId) && m.ExternalId != null)
            .ToDictionaryAsync(m => m.ExternalId!, ct);

        var count = 0;
        foreach (var apiMatch in fdResponse.Matches)
        {
            if (apiMatch.Competition?.Code is null) continue;
            if (!seasonByCode.ContainsKey(apiMatch.Competition.Code)) continue;
            if (!dbMatches.TryGetValue(apiMatch.Id.ToString(), out var match)) continue;

            var status = MapFdStatus(apiMatch.Status);
            if (status is MatchStatus.Live or MatchStatus.Finished)
            {
                match.HomeScore = apiMatch.Score.FullTime?.Home ?? match.HomeScore;
                match.AwayScore = apiMatch.Score.FullTime?.Away ?? match.AwayScore;
                match.Status = status;
                match.Minute = FormatFdMinute(apiMatch) ?? match.Minute;
                if (status == MatchStatus.Live) count++;
            }
        }

        return (count, null);
    }

    private static MatchStatus MapFdStatus(string apiStatus) => apiStatus switch
    {
        FootballDataApi.Status.Finished => MatchStatus.Finished,
        FootballDataApi.Status.InPlay or FootballDataApi.Status.Paused or FootballDataApi.Status.Live => MatchStatus.Live,
        FootballDataApi.Status.Postponed or FootballDataApi.Status.Suspended => MatchStatus.Postponed,
        FootballDataApi.Status.Cancelled => MatchStatus.Cancelled,
        _ => MatchStatus.Scheduled
    };

    private static string? FormatFdMinute(FootballDataMatch apiMatch) => apiMatch.Status switch
    {
        FootballDataApi.Status.Finished => PulseApi.DisplayLabels.FullTime,
        FootballDataApi.Status.Paused => PulseApi.DisplayLabels.HalfTime,
        FootballDataApi.Status.InPlay when apiMatch.Minute.HasValue =>
            apiMatch.InjuryTime > 0 ? $"{apiMatch.Minute}+{apiMatch.InjuryTime}'" : $"{apiMatch.Minute}'",
        _ => null
    };
}
