using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Constants;
using ScoreCast.Ws.Application.V1.MasterData.Commands;
using ScoreCast.Ws.Domain.V1.Entities;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.CommandHandlers;

internal sealed record SyncFplDataCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory,
    ILogger<SyncFplDataCommandHandler> Logger) : ICommandHandler<SyncFplDataCommand, ScoreCastResponse>
{
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
        try { bootstrap = await client.GetFromJsonAsync<FplBootstrapResponse>(FplApi.Routes.BootstrapStatic, ct); }
        catch (Exception ex) { return ScoreCastResponse.Error($"Failed to fetch FPL bootstrap: {ex.Message}"); }
        if (bootstrap is null) return ScoreCastResponse.Error("FPL bootstrap returned null.");

        // 2. Fetch all FPL fixtures
        List<FplFixture>? fixtures;
        try { fixtures = await client.GetFromJsonAsync<List<FplFixture>>(FplApi.Routes.Fixtures, ct); }
        catch (Exception ex) { return ScoreCastResponse.Error($"Failed to fetch FPL fixtures: {ex.Message}"); }
        if (fixtures is null or { Count: 0 }) return ScoreCastResponse.Error("No FPL fixtures returned.");

        // 3. Build FPL ID → code maps
        var fplTeamIdToCode = bootstrap.Teams.ToDictionary(t => t.Id, t => t.Code);

        // 4. Sync team mappings (FPL code → our team)
        var teamCodeToId = await SyncTeamMappingsAsync(bootstrap.Teams, competition, ct);

        // 5. Sync player mappings (FPL code → our player)
        await SyncPlayerMappingsAsync(bootstrap.Elements, fplTeamIdToCode, teamCodeToId, currentSeason, ct);

        // 6. Load our matches for this season (keyed by home + away team IDs — unique per season)
        var allMatches = await DbContext.Matches
            .Where(m => m.Gameweek.SeasonId == currentSeason.Id)
            .Select(m => new { m.Id, GameweekNumber = m.Gameweek.Number, m.GameweekId, m.HomeTeamId, m.AwayTeamId })
            .ToListAsync(ct);
        var matchLookup = allMatches.ToDictionary(m => (m.HomeTeamId, m.AwayTeamId), m => m);

        // Load gameweeks for potential reassignment
        var gameweeks = await DbContext.Gameweeks
            .Where(g => g.SeasonId == currentSeason.Id)
            .ToDictionaryAsync(g => g.Number, g => g.Id, ct);

        // 7. Process fixtures: reassign gameweeks where FPL disagrees
        var movedCount = 0;
        foreach (var fixture in fixtures.Where(f => f.Event.HasValue))
        {
            var homeTeamCode = fplTeamIdToCode.GetValueOrDefault(fixture.TeamH);
            var awayTeamCode = fplTeamIdToCode.GetValueOrDefault(fixture.TeamA);

            if (!teamCodeToId.TryGetValue(homeTeamCode, out var homeTeamId) ||
                !teamCodeToId.TryGetValue(awayTeamCode, out var awayTeamId))
                continue;

            if (!matchLookup.TryGetValue((homeTeamId, awayTeamId), out var match))
                continue;

            if (match.GameweekNumber != fixture.Event!.Value && gameweeks.TryGetValue(fixture.Event.Value, out var correctGwId))
            {
                var dbMatch = await DbContext.Matches.FindAsync([match.Id], ct);
                if (dbMatch is not null && dbMatch.GameweekId != correctGwId)
                {
                    dbMatch.GameweekId = correctGwId;
                    movedCount++;
                }
            }
        }

        // 8. Store pulse_id → match_id mappings for batch Pulse sync
        var existingPulseRefs = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Fpl && m.EntityType == EntityType.Match)
            .Select(m => m.EntityId)
            .ToListAsync(ct);
        var refMappedIds = existingPulseRefs.ToHashSet();

        foreach (var fixture in fixtures.Where(f => (f.Finished || f.Started == true) && f.PulseId.HasValue))
        {
            var homeTeamCode = fplTeamIdToCode.GetValueOrDefault(fixture.TeamH);
            var awayTeamCode = fplTeamIdToCode.GetValueOrDefault(fixture.TeamA);
            if (!teamCodeToId.TryGetValue(homeTeamCode, out var hId) ||
                !teamCodeToId.TryGetValue(awayTeamCode, out var aId))
                continue;
            if (!matchLookup.TryGetValue((hId, aId), out var m)) continue;
            if (!refMappedIds.Add(m.Id)) continue;

            DbContext.ExternalMappings.Add(new ExternalMapping
            {
                EntityType = EntityType.Match,
                EntityId = m.Id,
                Source = ExternalSource.Fpl,
                ExternalCode = fixture.PulseId!.Value.ToString()
            });
        }

        await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(SyncFplDataCommand), ct);
        return ScoreCastResponse.Ok($"Synced FPL mappings for {competition.Name}{(movedCount > 0 ? $", moved {movedCount} matches to correct gameweeks" : "")}.");
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
                (t.ShortName ?? "").Contains(fpl.ShortName, StringComparison.OrdinalIgnoreCase) ||
                MatchesAbbreviation(fpl.Name, t.Name));

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

    private async Task SyncPlayerMappingsAsync(
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
            .Select(tp => new { tp.PlayerId, tp.TeamId, tp.Player.Name, tp.Player.DateOfBirth })
            .ToListAsync(ct);

        var ourPlayersByTeam = ourPlayers
            .GroupBy(p => p.TeamId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var fpl in fplPlayers)
        {
            var code = fpl.Code.ToString();
            if (byCode.ContainsKey(code)) continue;

            var teamCode = fplTeamIdToCode.GetValueOrDefault(fpl.Team);
            if (!teamCodeToId.TryGetValue(teamCode, out var ourTeamId)) continue;
            if (!ourPlayersByTeam.TryGetValue(ourTeamId, out var teamPlayers)) continue;

            var fplFullName = $"{fpl.FirstName} {fpl.SecondName}";
            var match = teamPlayers.FirstOrDefault(p =>
                p.Name.Contains(fpl.SecondName, StringComparison.OrdinalIgnoreCase) ||
                fplFullName.Contains(p.Name, StringComparison.OrdinalIgnoreCase) ||
                p.Name.Contains(fpl.WebName, StringComparison.OrdinalIgnoreCase) ||
                fpl.WebName.Contains(p.Name, StringComparison.OrdinalIgnoreCase));

            match ??= fpl.BirthDate is not null
                ? teamPlayers.FirstOrDefault(p =>
                    p.DateOfBirth.HasValue &&
                    p.DateOfBirth.Value.ToString("yyyy-MM-dd") == fpl.BirthDate)
                : null;

            if (match is null || !byEntityId.Add(match.PlayerId)) continue;

            DbContext.ExternalMappings.Add(new ExternalMapping
            {
                EntityType = EntityType.Player,
                EntityId = match.PlayerId,
                Source = source,
                ExternalCode = code
            });
            byCode[code] = match.PlayerId;
        }
    }

    private static bool MatchesAbbreviation(string abbreviated, string fullName)
    {
        var abbrWords = abbreviated.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var fullWords = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (abbrWords.Length == 0) return false;
        return abbrWords.All(aw =>
            fullWords.Any(fw => fw.StartsWith(aw, StringComparison.OrdinalIgnoreCase)));
    }
}
