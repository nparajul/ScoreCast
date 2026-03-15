using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Constants;
using ScoreCast.Models.V1.Responses.MasterData;
using ScoreCast.Ws.Application.V1.MasterData.Commands;
using ScoreCast.Ws.Domain.V1.Entities;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.CommandHandlers;

internal sealed record SyncPulseEventsCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory,
    ILogger<SyncPulseEventsCommandHandler> Logger) : ICommandHandler<SyncPulseEventsCommand, ScoreCastResponse<SyncPulseEventsResult>>
{
    public async Task<ScoreCastResponse<SyncPulseEventsResult>> ExecuteAsync(SyncPulseEventsCommand command, CancellationToken ct)
    {
        try
        {
            return await ExecuteCoreAsync(command, ct);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Pulse events sync failed");
            return ScoreCastResponse<SyncPulseEventsResult>.Error($"Pulse sync failed: {ex.Message}");
        }
    }

    private async Task<ScoreCastResponse<SyncPulseEventsResult>> ExecuteCoreAsync(SyncPulseEventsCommand command, CancellationToken ct)
    {
        var competition = await DbContext.Competitions
            .FirstOrDefaultAsync(c => c.Code == command.Request.CompetitionCode, ct);
        if (competition is null)
            return ScoreCastResponse<SyncPulseEventsResult>.Error("Competition not found.");

        var currentSeason = await DbContext.Seasons
            .FirstOrDefaultAsync(s => s.CompetitionId == competition.Id && s.IsCurrent, ct);
        if (currentSeason is null)
            return ScoreCastResponse<SyncPulseEventsResult>.Error("No current season found.");

        // Get pulse_id → match_id from external_mapping (stored during FPL sync)
        var pulseRefRaw = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Fpl && m.EntityType == EntityType.Match)
            .Select(m => new { m.ExternalCode, m.EntityId })
            .ToListAsync(ct);
        var pulseRefMappings = pulseRefRaw.Select(m => new { PulseId = int.Parse(m.ExternalCode), MatchId = m.EntityId }).ToList();

        // Load matches with their team IDs and scores
        var matchDetails = await DbContext.Matches
            .Where(m => m.Gameweek.SeasonId == currentSeason.Id
                        && (m.Status == MatchStatus.Finished || m.Status == MatchStatus.Live))
            .Select(m => new { m.Id, m.HomeTeamId, m.AwayTeamId, m.Status })
            .ToDictionaryAsync(m => m.Id, ct);

        // Pulse-synced means ALL events were captured — safe to skip
        var pulseSyncedMatchIds = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Match)
            .Select(m => m.EntityId)
            .ToHashSetAsync(ct);

        // Build pending list — live always, finished only if not fully synced
        var pendingMatches = new List<(int PulseId, long MatchId, long HomeTeamId, long AwayTeamId)>();
        foreach (var pm in pulseRefMappings)
        {
            if (!matchDetails.TryGetValue(pm.MatchId, out var md)) continue;
            if (md.Status == MatchStatus.Finished && pulseSyncedMatchIds.Contains(pm.MatchId)) continue;
            pendingMatches.Add((pm.PulseId, pm.MatchId, md.HomeTeamId, md.AwayTeamId));
        }

        var total = pendingMatches.Count;
        var batch = pendingMatches.Take(command.Request.BatchSize).ToList();

        if (batch.Count == 0)
            return ScoreCastResponse<SyncPulseEventsResult>.Ok(
                new SyncPulseEventsResult(0, 0, 0, true), "All matches already synced.");

        // Load Pulse player mappings + our players for DOB matching
        var pulsePlayerMap = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Player)
            .ToDictionaryAsync(m => m.ExternalCode, m => m.EntityId, ct);

        var ourPlayersByTeam = await DbContext.TeamPlayers
            .Where(tp => tp.SeasonId == currentSeason.Id)
            .Select(tp => new { tp.PlayerId, tp.TeamId, tp.Player.Name, tp.Player.DateOfBirth })
            .ToListAsync(ct);
        var playersByTeam = ourPlayersByTeam.GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.Select(p => (p.PlayerId, p.Name, p.DateOfBirth)).ToList());
        var validPlayerIds = ourPlayersByTeam.Select(p => p.PlayerId).ToHashSet();

        var pulseClient = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.PulseClient));
        var eventCount = 0;
        var mappedEntityIds = pulsePlayerMap.Values.ToHashSet();

        // Fetch Pulse data in parallel (5 concurrent)
        using var semaphore = new SemaphoreSlim(5);
        var fetchTasks = batch.Select(async item =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(15));
                var data = await pulseClient.GetFromJsonAsync<PulseFixtureResponse>(string.Format(PulseApi.Routes.Fixture, item.PulseId), cts.Token);
                return (item, Data: data);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Pulse fetch failed for {PulseId}: {Error}", item.PulseId, ex.Message);
                return (item, Data: (PulseFixtureResponse?)null);
            }
            finally { semaphore.Release(); }
        }).ToList();

        var results = await Task.WhenAll(fetchTasks);

        // Load existing match events to avoid duplicates on re-sync
        var batchMatchIds = batch.Select(b => b.MatchId).ToHashSet();
        var existingEvents = batchMatchIds.Count > 0
            ? (await DbContext.MatchEvents
                .Where(e => batchMatchIds.Contains(e.MatchId))
                .Select(e => new { e.MatchId, e.PlayerId, e.EventType, e.Minute })
                .ToListAsync(ct))
                .Select(e => $"{e.MatchId}|{e.PlayerId}|{e.EventType}|{e.Minute}")
                .ToHashSet()
            : [];

        // Process results sequentially (DB context not thread-safe)
        foreach (var (item, pulseData) in results)
        {
            var (pulseId, matchId, homeTeamId, awayTeamId) = item;
            var isLive = matchDetails[matchId].Status == MatchStatus.Live;

            if (pulseData?.Events is null) continue;

            // Update venue from Pulse (more accurate than football-data.org)
            if (pulseData.Ground?.Name is { } venueName)
            {
                var dbMatch = await DbContext.Matches.FindAsync([matchId], ct);
                if (dbMatch is not null) dbMatch.Venue = venueName;
            }

            MapPulsePlayers(pulseData, homeTeamId, awayTeamId, playersByTeam, pulsePlayerMap, mappedEntityIds);

            var seenKeys = new HashSet<(long, long, MatchEventType, string?)>();
            var eventsExpected = pulseData.Events.Count(pe => pe.PersonId is not null && MapPulseEvent(pe.Type, pe.Description) is not null);
            var eventsMatched = 0;

            foreach (var pe in pulseData.Events)
            {
                if (pe.PersonId is null) continue;
                var eventType = MapPulseEvent(pe.Type, pe.Description);
                if (eventType is null) continue;

                var personKey = pe.PersonId.Value.ToString();
                if (!pulsePlayerMap.TryGetValue(personKey, out var playerId)) continue;
                if (!validPlayerIds.Contains(playerId)) continue;

                var minute = pe.Clock?.Label?.Replace("'00", "'");
                if (!seenKeys.Add((matchId, playerId, eventType.Value, minute))) continue;

                if (existingEvents.Contains($"{matchId}|{playerId}|{eventType.Value}|{minute}"))
                {
                    eventsMatched++;
                    continue;
                }

                DbContext.MatchEvents.Add(new MatchEvent
                {
                    MatchId = matchId, PlayerId = playerId,
                    EventType = eventType.Value, Value = 1, Minute = minute
                });
                eventCount++;
                eventsMatched++;

                // Create assist event if goal has an assistId
                if (pe.AssistId is not null && (eventType is MatchEventType.Goal or MatchEventType.PenaltyGoal))
                {
                    var assistKey = pe.AssistId.Value.ToString();
                    if (pulsePlayerMap.TryGetValue(assistKey, out var assistPlayerId) && validPlayerIds.Contains(assistPlayerId))
                    {
                        if (seenKeys.Add((matchId, assistPlayerId, MatchEventType.Assist, minute))
                            && !existingEvents.Contains($"{matchId}|{assistPlayerId}|{MatchEventType.Assist}|{minute}"))
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

            // Only mark as Pulse-synced when ALL events were successfully mapped
            if (!isLive && !pulseSyncedMatchIds.Contains(matchId) && eventsMatched >= eventsExpected)
            {
                DbContext.ExternalMappings.Add(new ExternalMapping
                {
                    EntityType = EntityType.Match,
                    EntityId = matchId,
                    Source = ExternalSource.Pulse,
                    ExternalCode = pulseId.ToString()
                });
            }
        }

        await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(SyncPulseEventsCommand), ct);

        var complete = batch.Count >= total;
        return ScoreCastResponse<SyncPulseEventsResult>.Ok(
            new SyncPulseEventsResult(batch.Count, total, eventCount, complete),
            $"Processed {batch.Count}/{total} matches, {eventCount} events added.");
    }

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

    private void MapPulsePlayers(
        PulseFixtureResponse pulse,
        long homeTeamId,
        long awayTeamId,
        Dictionary<long, List<(long PlayerId, string Name, DateOnly? DateOfBirth)>> ourPlayersByTeam,
        Dictionary<string, long> pulsePlayerMap,
        HashSet<long> mappedEntityIds)
    {
        if (pulse.TeamLists is null) return;

        var teamIds = new[] { homeTeamId, awayTeamId };
        for (var i = 0; i < pulse.TeamLists.Count && i < 2; i++)
        {
            var tl = pulse.TeamLists[i];
            var teamId = teamIds[i];
            if (!ourPlayersByTeam.TryGetValue(teamId, out var teamPlayers)) continue;

            var allPlayers = (tl.Lineup ?? []).Concat(tl.Substitutes ?? []);
            foreach (var pp in allPlayers)
            {
                var key = pp.Id.ToString();
                if (pulsePlayerMap.ContainsKey(key)) continue;

                var displayName = pp.Name?.Display;
                if (displayName is null) continue;

                DateOnly? pulseDob = null;
                if (pp.Birth?.Date?.Millis is long millis)
                    pulseDob = DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(millis).UtcDateTime);

                var match = pulseDob.HasValue
                    ? teamPlayers.FirstOrDefault(p =>
                        p.DateOfBirth.HasValue && p.DateOfBirth.Value == pulseDob.Value)
                    : ((long PlayerId, string Name, DateOnly? DateOfBirth)?)null;

                match ??= teamPlayers.FirstOrDefault(p =>
                    p.Name.Contains(pp.Name?.Last ?? "§", StringComparison.OrdinalIgnoreCase) ||
                    displayName.Contains(p.Name, StringComparison.OrdinalIgnoreCase));

                if (match is not { } m) continue;
                if (!mappedEntityIds.Add(m.PlayerId)) { pulsePlayerMap[key] = m.PlayerId; break; }

                pulsePlayerMap[key] = m.PlayerId;
                DbContext.ExternalMappings.Add(new ExternalMapping
                {
                    EntityType = EntityType.Player,
                    EntityId = m.PlayerId,
                    Source = ExternalSource.Pulse,
                    ExternalCode = key
                });
            }
        }
    }
}
