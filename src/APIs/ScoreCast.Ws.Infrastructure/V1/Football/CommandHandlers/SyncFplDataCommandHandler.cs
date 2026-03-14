using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Application.V1.Football.Commands;
using ScoreCast.Ws.Domain.V1.Entities;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Domain.V1.Enums;
using ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

namespace ScoreCast.Ws.Infrastructure.V1.Football.CommandHandlers;

internal sealed record SyncFplDataCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory) : ICommandHandler<SyncFplDataCommand, ScoreCastResponse>
{
    private static readonly Dictionary<string, MatchEventType> EventTypeMap = new()
    {
        ["goals_scored"] = MatchEventType.Goal,
        ["assists"] = MatchEventType.Assist,
        ["own_goals"] = MatchEventType.OwnGoal,
        ["yellow_cards"] = MatchEventType.YellowCard,
        ["red_cards"] = MatchEventType.RedCard,
        ["penalties_saved"] = MatchEventType.PenaltySaved,
        ["penalties_missed"] = MatchEventType.PenaltyMissed
    };

    public async Task<ScoreCastResponse> ExecuteAsync(SyncFplDataCommand command, CancellationToken ct)
    {
        var competition = await DbContext.Competitions
            .FirstOrDefaultAsync(c => c.Code == command.Request.CompetitionCode, ct);

        if (competition is null)
            return ScoreCastResponse.Error($"Competition {command.Request.CompetitionCode} not found.");

        var currentSeason = await DbContext.Seasons
            .FirstOrDefaultAsync(s => s.CompetitionId == competition.Id && s.IsCurrent, ct);

        if (currentSeason is null)
            return ScoreCastResponse.Error("No current season found.");

        var client = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FplClient));

        // 1. Fetch FPL bootstrap (teams + players)
        FplBootstrapResponse? bootstrap;
        try
        {
            bootstrap = await client.GetFromJsonAsync<FplBootstrapResponse>(FplApi.Routes.BootstrapStatic, ct);
        }
        catch (Exception ex)
        {
            return ScoreCastResponse.Error($"Failed to fetch FPL bootstrap: {ex.Message}");
        }

        if (bootstrap is null)
            return ScoreCastResponse.Error("FPL bootstrap returned null.");

        // 2. Fetch all FPL fixtures
        List<FplFixture>? fixtures;
        try
        {
            fixtures = await client.GetFromJsonAsync<List<FplFixture>>(FplApi.Routes.Fixtures, ct);
        }
        catch (Exception ex)
        {
            return ScoreCastResponse.Error($"Failed to fetch FPL fixtures: {ex.Message}");
        }

        if (fixtures is null or { Count: 0 })
            return ScoreCastResponse.Error("No FPL fixtures returned.");

        // 3. Build FPL ID → code maps
        var fplTeamIdToCode = bootstrap.Teams.ToDictionary(t => t.Id, t => t.Code);
        var fplPlayerIdToCode = bootstrap.Elements.ToDictionary(p => p.Id, p => p.Code);
        var fplPlayerCodeToName = bootstrap.Elements.ToDictionary(p => p.Code, p => $"{p.FirstName} {p.SecondName}");

        // 4. Sync team mappings (FPL code → our team)
        var teamCodeToId = await SyncTeamMappingsAsync(bootstrap.Teams, competition, ct);

        // 5. Sync player mappings (FPL code → our player)
        var playerCodeToId = await SyncPlayerMappingsAsync(bootstrap.Elements, fplTeamIdToCode, teamCodeToId, currentSeason, ct);

        // 6. Load our matches for this season (keyed by gameweek + home + away team IDs)
        var matches = await DbContext.Matches
            .Where(m => m.Gameweek.SeasonId == currentSeason.Id && m.Status == MatchStatus.Finished)
            .Select(m => new { m.Id, m.Gameweek.Number, m.HomeTeamId, m.AwayTeamId })
            .ToListAsync(ct);

        var matchLookup = matches.ToDictionary(
            m => (m.Number, m.HomeTeamId, m.AwayTeamId), m => m.Id);

        // 7. Load existing match events to avoid duplicates
        var existingEvents = await DbContext.MatchEvents
            .Where(e => matches.Select(m => m.Id).Contains(e.MatchId))
            .Select(e => new { e.MatchId, e.PlayerId, e.EventType })
            .ToHashSetAsync(ct);

        // 8. Process fixtures and create match events
        await using var transaction = await UnitOfWork.BeginTransactionAsync(ct);
        try
        {
            var eventCount = 0;
            foreach (var fixture in fixtures.Where(f => f.Finished && f.Event.HasValue && f.Stats.Count > 0))
            {
                var homeTeamCode = fplTeamIdToCode.GetValueOrDefault(fixture.TeamH);
                var awayTeamCode = fplTeamIdToCode.GetValueOrDefault(fixture.TeamA);

                if (!teamCodeToId.TryGetValue(homeTeamCode, out var homeTeamId) ||
                    !teamCodeToId.TryGetValue(awayTeamCode, out var awayTeamId))
                    continue;

                if (!matchLookup.TryGetValue((fixture.Event!.Value, homeTeamId, awayTeamId), out var matchId))
                    continue;

                foreach (var stat in fixture.Stats)
                {
                    if (!EventTypeMap.TryGetValue(stat.Identifier, out var eventType))
                        continue;

                    foreach (var entry in stat.H.Concat(stat.A))
                    {
                        var playerCode = fplPlayerIdToCode.GetValueOrDefault(entry.Element);
                        if (!playerCodeToId.TryGetValue(playerCode, out var playerId))
                            continue;

                        var key = new { MatchId = matchId, PlayerId = playerId, EventType = eventType };
                        if (!existingEvents.Add(key))
                            continue;

                        DbContext.MatchEvents.Add(new MatchEvent
                        {
                            MatchId = matchId,
                            PlayerId = playerId,
                            EventType = eventType,
                            Value = entry.Value
                        });
                        eventCount++;
                    }
                }
            }

            await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(SyncFplDataCommand), ct);
            await transaction.CommitAsync(ct);
            return ScoreCastResponse.Ok($"Synced {eventCount} match events from FPL for {competition.Name}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return ScoreCastResponse.Error($"Failed to sync FPL data: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private async Task<Dictionary<int, long>> SyncTeamMappingsAsync(
        List<FplTeam> fplTeams, Competition competition, CancellationToken ct)
    {
        var source = ExternalSource.Fpl;
        var existing = await DbContext.ExternalMappings
            .Where(m => m.Source == source && m.EntityType == EntityType.Team)
            .ToListAsync(ct);

        var byCode = existing.ToDictionary(m => m.ExternalCode, m => m.EntityId);
        var byEntityId = existing.Select(m => m.EntityId).ToHashSet();

        var ourTeams = await DbContext.SeasonTeams
            .Where(st => st.Season.CompetitionId == competition.Id && st.Season.IsCurrent)
            .Select(st => new { st.Team.Id, st.Team.Name, st.Team.ShortName })
            .ToListAsync(ct);

        var result = new Dictionary<int, long>();

        foreach (var fpl in fplTeams)
        {
            var code = fpl.Code.ToString();
            if (byCode.TryGetValue(code, out var entityId))
            {
                result[fpl.Code] = entityId;
                continue;
            }

            var match = ourTeams.FirstOrDefault(t =>
                t.Name.Contains(fpl.Name, StringComparison.OrdinalIgnoreCase) ||
                fpl.Name.Contains(t.ShortName ?? "", StringComparison.OrdinalIgnoreCase) ||
                (t.ShortName ?? "").Contains(fpl.ShortName, StringComparison.OrdinalIgnoreCase));

            if (match is null || !byEntityId.Add(match.Id)) continue;

            DbContext.ExternalMappings.Add(new ExternalMapping
            {
                EntityType = EntityType.Team,
                EntityId = match.Id,
                Source = source,
                ExternalCode = code
            });
            byCode[code] = match.Id;
            result[fpl.Code] = match.Id;
        }

        return result;
    }

    private async Task<Dictionary<int, long>> SyncPlayerMappingsAsync(
        List<FplPlayer> fplPlayers, Dictionary<int, int> fplTeamIdToCode,
        Dictionary<int, long> teamCodeToId, Season season, CancellationToken ct)
    {
        var source = ExternalSource.Fpl;
        var existing = await DbContext.ExternalMappings
            .Where(m => m.Source == source && m.EntityType == EntityType.Player)
            .ToListAsync(ct);

        var byCode = existing.ToDictionary(m => m.ExternalCode, m => m.EntityId);
        var byEntityId = existing.Select(m => m.EntityId).ToHashSet();

        var ourPlayers = await DbContext.TeamPlayers
            .Where(tp => tp.SeasonId == season.Id)
            .Select(tp => new { tp.PlayerId, tp.TeamId, tp.Player.Name })
            .ToListAsync(ct);

        var ourPlayersByTeam = ourPlayers
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new Dictionary<int, long>();

        foreach (var fpl in fplPlayers)
        {
            var code = fpl.Code.ToString();
            if (byCode.TryGetValue(code, out var entityId))
            {
                result[fpl.Code] = entityId;
                continue;
            }

            var teamCode = fplTeamIdToCode.GetValueOrDefault(fpl.Team);
            if (!teamCodeToId.TryGetValue(teamCode, out var ourTeamId))
                continue;

            if (!ourPlayersByTeam.TryGetValue(ourTeamId, out var teamPlayers))
                continue;

            var fplFullName = $"{fpl.FirstName} {fpl.SecondName}";
            var match = teamPlayers.FirstOrDefault(p =>
                p.Name.Contains(fpl.SecondName, StringComparison.OrdinalIgnoreCase) ||
                fplFullName.Contains(p.Name, StringComparison.OrdinalIgnoreCase));

            if (match is null || !byEntityId.Add(match.PlayerId)) continue;

            DbContext.ExternalMappings.Add(new ExternalMapping
            {
                EntityType = EntityType.Player,
                EntityId = match.PlayerId,
                Source = source,
                ExternalCode = code
            });
            byCode[code] = match.PlayerId;
            result[fpl.Code] = match.PlayerId;
        }

        return result;
    }
}
