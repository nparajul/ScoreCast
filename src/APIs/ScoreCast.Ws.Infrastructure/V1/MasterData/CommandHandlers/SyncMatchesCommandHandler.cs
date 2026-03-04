using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Exceptions;
using ScoreCast.Ws.Application.V1.MasterData.Commands;
using ScoreCast.Ws.Domain.V1.Entities;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Infrastructure.V1.MasterData.ExternalModels;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.CommandHandlers;

internal sealed record SyncMatchesCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory) : ICommandHandler<SyncMatchesCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(SyncMatchesCommand command, CancellationToken ct)
    {
        var competition = await DbContext.Competitions
            .Include(c => c.Country)
            .FirstOrDefaultAsync(c => c.Code == command.Request.CompetitionCode, ct);

        if (competition is null)
            return ScoreCastResponse.Error($"Competition {command.Request.CompetitionCode} not found. Sync the competition first.");

        var seasonsQuery = DbContext.Seasons
            .Where(s => s.CompetitionId == competition.Id);

        if (!command.Request.SyncAll)
            seasonsQuery = seasonsQuery.Where(s => s.IsCurrent);

        var seasons = await seasonsQuery
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(ct);

        if (seasons.Count == 0)
            return ScoreCastResponse.Error("No seasons found. Sync the competition first.");

        var isPremierLeague = command.Request.CompetitionCode == CompetitionCodes.PremierLeague;

        await using var transaction = await UnitOfWork.BeginTransactionAsync(ct);
        try
        {
            var teamCache = await DbContext.Teams
                .Where(t => t.ExternalId != null)
                .ToDictionaryAsync(t => t.ExternalId!, ct);

            var totalMatches = 0;

            var warnings = new List<string>();

            if (isPremierLeague)
                totalMatches = await SyncFromPulseAsync(command.Request.AppName ?? nameof(SyncMatchesCommand), seasons, competition.Country, teamCache, ct);

            // Fallback to football-data.org only for non-PL competitions
            if (!isPremierLeague)
            {
                var (count, w) = await SyncFromFootballDataAsync(command.Request.CompetitionCode, seasons, competition.Country, teamCache, ct);
                totalMatches = count;
                warnings = w;
            }

            await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(SyncMatchesCommand), ct);

            // Sync events for non-PL finished matches without events
            var eventCount = 0;
            if (!isPremierLeague)
            {
                var seasonIds = seasons.Select(s => s.Id).ToList();
                eventCount = await SyncFdMatchEventsAsync(seasonIds, ct);
                if (eventCount > 0)
                    await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(SyncMatchesCommand), ct);
            }

            await transaction.CommitAsync(ct);
            var msg = $"Synced {totalMatches} matches, {eventCount} events across {seasons.Count} seasons for {competition.Name}";
            if (warnings.Count > 0) msg += $". Skipped: {string.Join("; ", warnings)}";
            return ScoreCastResponse.Ok(msg);
        }
        catch (ScoreCastException ex)
        {
            await transaction.RollbackAsync(ct);
            return ScoreCastResponse.Error(ex.Message);
        }
    }

    private async Task<int> SyncFromPulseAsync(
        string appName, List<Season> seasons, Country country, Dictionary<string, Team> teamCache, CancellationToken ct)
    {
        var pulseClient = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.PulseClient));

        // Build Pulse team ID → our Team lookup
        var pulseTeamMap = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Team)
            .ToDictionaryAsync(m => m.ExternalCode, m => m.EntityId, ct);

        var pulseTeamIds = pulseTeamMap.Values.ToHashSet();
        var teamById = await DbContext.Teams
            .Where(t => pulseTeamIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

        var totalMatches = 0;

        foreach (var season in seasons)
        {
            // Get Pulse compSeason ID from external_mapping
            var pulseCompSeasonId = await DbContext.ExternalMappings
                .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Season
                            && m.EntityId == season.Id)
                .Select(m => m.ExternalCode)
                .FirstOrDefaultAsync(ct);

            if (pulseCompSeasonId is null) continue;

            PulseFixturesListResponse? response;
            try
            {
                response = await pulseClient.GetFromJsonAsync<PulseFixturesListResponse>(
                    string.Format(PulseApi.Routes.FixturesByCompSeason, pulseCompSeasonId), ct);
            }
            catch (Exception ex)
            {
                throw new ScoreCastException($"Pulse fixtures API call failed for compSeason {pulseCompSeasonId}", ex);
            }

            if (response?.Content is not { Count: > 0 }) continue;

            var (matchCount, pendingMappings) = await UpsertPulseMatchesAsync(season, country, response.Content, pulseTeamMap, teamById, teamCache, ct);
            totalMatches += matchCount;

            // Flush to assign match IDs, then create mappings with real IDs
            if (pendingMappings.Count > 0)
            {
                await UnitOfWork.SaveChangesAsync(appName, ct);
                foreach (var (match, pulseFixtureId) in pendingMappings)
                {
                    DbContext.ExternalMappings.Add(new ExternalMapping
                    {
                        EntityType = EntityType.Match,
                        EntityId = match.Id,
                        Source = ExternalSource.Pulse,
                        ExternalCode = pulseFixtureId
                    });
                }
            }
        }

        return totalMatches;
    }

    private async Task<(int Count, List<(Match Match, string PulseFixtureId)> PendingMappings)> UpsertPulseMatchesAsync(
        Season season, Country country, List<PulseFixtureListItem> fixtures,
        Dictionary<string, long> pulseTeamMap, Dictionary<long, Team> teamById,
        Dictionary<string, Team> teamCache, CancellationToken ct)
    {
        var gameweekCache = await DbContext.Gameweeks
            .Where(g => g.SeasonId == season.Id)
            .ToDictionaryAsync(g => g.Number, ct);

        // Build Pulse fixture ID → our match lookup
        var existingPulseMappings = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Match)
            .ToDictionaryAsync(m => m.ExternalCode, m => m.EntityId, ct);

        var existingMatches = await DbContext.Matches
            .Where(m => m.Gameweek.SeasonId == season.Id)
            .ToDictionaryAsync(m => m.Id, ct);

        var count = 0;
        var pendingMappings = new List<(Match Match, string PulseFixtureId)>();
        foreach (var pf in fixtures)
        {
            var pulseFixtureId = ((int)pf.Id).ToString();
            var matchday = (int)(pf.Gameweek?.Gameweek ?? 1);

            if (!gameweekCache.TryGetValue(matchday, out var gameweek))
            {
                gameweek = new Gameweek { SeasonId = season.Id, Number = matchday };
                DbContext.Gameweeks.Add(gameweek);
                gameweekCache[matchday] = gameweek;
            }

            // Resolve teams
            if (pf.Teams is not { Count: 2 }) continue;
            var homePulseId = pf.Teams[0].Team?.Id.ToString();
            var awayPulseId = pf.Teams[1].Team?.Id.ToString();
            if (homePulseId is null || awayPulseId is null) continue;

            if (!pulseTeamMap.TryGetValue(homePulseId, out var homeTeamId) ||
                !pulseTeamMap.TryGetValue(awayPulseId, out var awayTeamId))
                continue;

            if (!teamById.TryGetValue(homeTeamId, out var homeTeam) ||
                !teamById.TryGetValue(awayTeamId, out var awayTeam))
                continue;

            var kickoff = pf.Kickoff?.Millis is not null
                ? DateTimeOffset.FromUnixTimeMilliseconds((long)pf.Kickoff.Millis.Value).UtcDateTime
                : (DateTime?)null;

            var status = MapPulseStatus(pf.Status);
            var minute = FormatPulseMinute(pf);
            var referee = pf.MatchOfficials?.FirstOrDefault(o => o.Role == SharedConstants.RefereeRole)?.Name?.Display;

            // Check if match already exists via Pulse mapping
            Match? match = null;
            if (existingPulseMappings.TryGetValue(pulseFixtureId, out var existingMatchId))
                existingMatches.TryGetValue(existingMatchId, out match);

            // Also try to find by home/away team in same gameweek
            match ??= existingMatches.Values.FirstOrDefault(m =>
                m.HomeTeamId == homeTeamId && m.AwayTeamId == awayTeamId);

            if (match is null)
            {
                match = new Match
                {
                    Gameweek = gameweek,
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    KickoffTime = kickoff,
                    HomeScore = (int?)pf.Teams[0].Score,
                    AwayScore = (int?)pf.Teams[1].Score,
                    Status = status,
                    Venue = homeTeam.Venue,
                    Referee = referee,
                    Minute = minute
                };
                DbContext.Matches.Add(match);
                existingMatches[match.Id] = match;
            }
            else
            {
                match.KickoffTime = kickoff;
                match.HomeScore = (int?)pf.Teams[0].Score;
                match.AwayScore = (int?)pf.Teams[1].Score;
                match.Status = status;
                match.Venue = homeTeam.Venue;
                match.Referee = referee;
                match.Minute = minute;
            }

            // Track Pulse mapping for new matches (deferred until IDs are assigned)
            if (!existingPulseMappings.ContainsKey(pulseFixtureId))
                pendingMappings.Add((match, pulseFixtureId));

            // Update gameweek dates
            if (kickoff.HasValue)
            {
                var matchDate = DateOnly.FromDateTime(kickoff.Value);
                if (gameweek.StartDate is null || matchDate < gameweek.StartDate)
                    gameweek.StartDate = matchDate;
                if (gameweek.EndDate is null || matchDate > gameweek.EndDate)
                    gameweek.EndDate = matchDate;
            }

            count++;
        }

        // Update gameweek statuses
        foreach (var gw in gameweekCache.Values)
        {
            var matches = await DbContext.Matches
                .Where(m => m.GameweekId == gw.Id || m.Gameweek == gw)
                .ToListAsync(ct);

            if (matches.Count == 0) continue;
            gw.Status = matches.All(m => m.Status == MatchStatus.Finished) ? GameweekStatus.Completed
                : matches.Any(m => m.Status is MatchStatus.Live or MatchStatus.Finished) ? GameweekStatus.Active
                : GameweekStatus.Upcoming;
        }

        return (count, pendingMappings);
    }

    private async Task<(int Total, List<string> Warnings)> SyncFromFootballDataAsync(
        string competitionCode, List<Season> seasons, Country country,
        Dictionary<string, Team> teamCache, CancellationToken ct)
    {
        var client = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FootballDataClient));
        var totalMatches = 0;
        var warnings = new List<string>();

        foreach (var season in seasons)
        {
            var seasonYear = season.StartDate.Year;
            FootballDataMatchesResponse? apiResponse;
            try
            {
                apiResponse = await client.GetFromJsonAsync<FootballDataMatchesResponse>(
                    string.Format(FootballDataApi.Routes.Matches, competitionCode, seasonYear), ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.TooManyRequests)
            {
                warnings.Add($"{competitionCode} {seasonYear}: {(ex.StatusCode == System.Net.HttpStatusCode.Forbidden ? "Restricted" : "Rate limited")}");
                continue;
            }
            catch (Exception ex)
            {
                throw new ScoreCastException($"Football-data.org matches API failed for {competitionCode} season {seasonYear}", ex);
            }

            if (apiResponse?.Matches is { Count: > 0 })
                totalMatches += await UpsertFdMatchesForSeasonAsync(season, country, apiResponse.Matches, teamCache, ct);
        }

        return (totalMatches, warnings);
    }

    private async Task<int> UpsertFdMatchesForSeasonAsync(
        Season season, Country country, List<FootballDataMatch> apiMatches,
        Dictionary<string, Team> teamCache, CancellationToken ct)
    {
        var gameweekCache = new Dictionary<int, Gameweek>();
        var existingGameweeks = await DbContext.Gameweeks
            .Where(g => g.SeasonId == season.Id)
            .ToDictionaryAsync(g => g.Number, ct);

        foreach (var kvp in existingGameweeks)
            gameweekCache[kvp.Key] = kvp.Value;

        var existingMatches = await DbContext.Matches
            .Where(m => m.Gameweek.SeasonId == season.Id && m.ExternalId != null)
            .ToDictionaryAsync(m => m.ExternalId!, ct);

        var count = 0;
        foreach (var apiMatch in apiMatches)
        {
            if (apiMatch.HomeTeam.Id is null || apiMatch.AwayTeam.Id is null) continue;

            var matchday = apiMatch.Matchday ?? 1;
            if (!gameweekCache.TryGetValue(matchday, out var gameweek))
            {
                gameweek = new Gameweek { SeasonId = season.Id, Number = matchday };
                DbContext.Gameweeks.Add(gameweek);
                gameweekCache[matchday] = gameweek;
            }

            var homeTeam = EnsureTeam(teamCache, apiMatch.HomeTeam, country);
            var awayTeam = EnsureTeam(teamCache, apiMatch.AwayTeam, country);

            var externalId = apiMatch.Id.ToString();
            existingMatches.TryGetValue(externalId, out var match);

            var kickoff = DateTimeOffset.TryParse(apiMatch.UtcDate, out var dto)
                ? dto.UtcDateTime
                : (DateTime?)null;

            var status = MapFdStatus(apiMatch.Status);
            var minute = FormatFdMinute(apiMatch);
            var referee = apiMatch.Referees?.FirstOrDefault(r => r.Type == "REFEREE")?.Name;

            if (match is null)
            {
                match = new Match
                {
                    Gameweek = gameweek,
                    HomeTeam = homeTeam,
                    AwayTeam = awayTeam,
                    ExternalId = externalId,
                    KickoffTime = kickoff,
                    HomeScore = apiMatch.Score.FullTime?.Home,
                    AwayScore = apiMatch.Score.FullTime?.Away,
                    Status = status,
                    Venue = homeTeam.Venue,
                    Referee = referee,
                    Minute = minute
                };
                DbContext.Matches.Add(match);
            }
            else
            {
                match.KickoffTime = kickoff;
                match.HomeScore = apiMatch.Score.FullTime?.Home;
                match.AwayScore = apiMatch.Score.FullTime?.Away;
                match.Status = status;
                match.Venue = homeTeam.Venue;
                match.Referee = referee;
                match.Minute = minute;
            }

            if (kickoff.HasValue)
            {
                var matchDate = DateOnly.FromDateTime(kickoff.Value);
                if (gameweek.StartDate is null || matchDate < gameweek.StartDate)
                    gameweek.StartDate = matchDate;
                if (gameweek.EndDate is null || matchDate > gameweek.EndDate)
                    gameweek.EndDate = matchDate;
            }

            count++;
        }

        foreach (var gw in gameweekCache.Values)
        {
            var matches = await DbContext.Matches
                .Where(m => m.GameweekId == gw.Id || m.Gameweek == gw)
                .ToListAsync(ct);

            if (matches.Count == 0) continue;
            gw.Status = matches.All(m => m.Status == MatchStatus.Finished) ? GameweekStatus.Completed
                : matches.Any(m => m.Status is MatchStatus.Live or MatchStatus.Finished) ? GameweekStatus.Active
                : GameweekStatus.Upcoming;
        }

        return count;
    }

    private static MatchStatus MapPulseStatus(string status) => status switch
    {
        PulseApi.Status.Complete => MatchStatus.Finished,
        PulseApi.Status.Live => MatchStatus.Live,
        _ => MatchStatus.Scheduled
    };

    private static string? FormatPulseMinute(PulseFixtureListItem pf) => pf.Status switch
    {
        PulseApi.Status.Complete => PulseApi.DisplayLabels.FullTime,
        PulseApi.Status.Live when pf.Phase == PulseApi.Phase.HalfTime => PulseApi.DisplayLabels.HalfTime,
        PulseApi.Status.Live when pf.Clock is not null => pf.Clock.Label.Replace("'00", "'"),
        _ => null
    };

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

    private Team EnsureTeam(Dictionary<string, Team> teamCache, FootballDataMatchTeam apiTeam, Country country)
    {
        var externalId = apiTeam.Id!.Value.ToString();
        if (teamCache.TryGetValue(externalId, out var team))
            return team;

        team = new Team
        {
            Name = apiTeam.Name ?? "Unknown",
            ShortName = apiTeam.ShortName,
            LogoUrl = apiTeam.Crest,
            ExternalId = externalId,
            Country = country
        };
        DbContext.Teams.Add(team);
        teamCache[externalId] = team;
        return team;
    }

    private async Task<int> SyncFdMatchEventsAsync(List<long> seasonIds, CancellationToken ct)
    {
        // Find finished matches with no events
        var matchesWithEvents = await DbContext.MatchEvents
            .Where(e => seasonIds.Contains(e.Match.Gameweek.SeasonId))
            .Select(e => e.MatchId)
            .Distinct()
            .ToListAsync(ct);
        var hasEvents = matchesWithEvents.ToHashSet();

        var finishedMatches = await DbContext.Matches
            .Where(m => seasonIds.Contains(m.Gameweek.SeasonId)
                        && m.Status == MatchStatus.Finished
                        && m.ExternalId != null)
            .Select(m => new { m.Id, m.ExternalId, m.HomeTeamId, m.AwayTeamId })
            .ToListAsync(ct);

        var pending = finishedMatches.Where(m => !hasEvents.Contains(m.Id)).ToList();
        if (pending.Count == 0) return 0;

        // Player lookup by external ID
        var playerMap = await DbContext.Players
            .Where(p => p.ExternalId != null)
            .ToDictionaryAsync(p => p.ExternalId!, p => p.Id, ct);

        // Team lookup by external ID
        var teamMap = await DbContext.Teams
            .Where(t => t.ExternalId != null)
            .ToDictionaryAsync(t => t.ExternalId!, t => t.Id, ct);

        var client = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FootballDataClient));
        var eventCount = 0;

        // Rate limit: 10 req/min on free tier — fetch up to 9 per sync run
        foreach (var match in pending.Take(9))
        {
            FootballDataMatchDetailResponse? detail;
            try
            {
                detail = await client.GetFromJsonAsync<FootballDataMatchDetailResponse>(
                    string.Format(FootballDataApi.Routes.MatchDetail, match.ExternalId), ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Forbidden or System.Net.HttpStatusCode.TooManyRequests)
            {
                break; // stop fetching, hit rate limit
            }
            catch { continue; }

            if (detail?.Match is null) continue;

            var homeTeamFdId = detail.Match.HomeTeam.Id?.ToString();
            var homeTeamId = homeTeamFdId is not null && teamMap.TryGetValue(homeTeamFdId, out var htId) ? htId : match.HomeTeamId;

            // Goals
            foreach (var g in detail.Match.Goals ?? [])
            {
                if (g.Scorer?.Id is null) continue;
                if (!playerMap.TryGetValue(g.Scorer.Id.Value.ToString(), out var scorerId)) continue;

                var eventType = g.Type switch
                {
                    "OWN" => MatchEventType.OwnGoal,
                    "PENALTY" => MatchEventType.PenaltyGoal,
                    _ => MatchEventType.Goal
                };
                var minute = FormatMinute(g.Minute, g.ExtraTime);

                DbContext.MatchEvents.Add(new MatchEvent
                {
                    MatchId = match.Id, PlayerId = scorerId,
                    EventType = eventType, Value = 1, Minute = minute
                });
                eventCount++;

                // Assist
                if (g.Assist?.Id is not null && playerMap.TryGetValue(g.Assist.Id.Value.ToString(), out var assistId))
                {
                    DbContext.MatchEvents.Add(new MatchEvent
                    {
                        MatchId = match.Id, PlayerId = assistId,
                        EventType = MatchEventType.Assist, Value = 1, Minute = minute
                    });
                    eventCount++;
                }
            }

            // Bookings
            foreach (var b in detail.Match.Bookings ?? [])
            {
                if (b.Player?.Id is null) continue;
                if (!playerMap.TryGetValue(b.Player.Id.Value.ToString(), out var playerId)) continue;

                var eventType = b.Card switch
                {
                    "RED_CARD" => MatchEventType.RedCard,
                    "YELLOW_RED_CARD" => MatchEventType.RedCard,
                    _ => MatchEventType.YellowCard
                };

                DbContext.MatchEvents.Add(new MatchEvent
                {
                    MatchId = match.Id, PlayerId = playerId,
                    EventType = eventType, Value = 1, Minute = FormatMinute(b.Minute, null)
                });
                eventCount++;
            }

            // Substitutions
            foreach (var s in detail.Match.Substitutions ?? [])
            {
                if (s.PlayerIn?.Id is not null && playerMap.TryGetValue(s.PlayerIn.Id.Value.ToString(), out var inId))
                {
                    DbContext.MatchEvents.Add(new MatchEvent
                    {
                        MatchId = match.Id, PlayerId = inId,
                        EventType = MatchEventType.SubIn, Value = 1, Minute = FormatMinute(s.Minute, null)
                    });
                    eventCount++;
                }
                if (s.PlayerOut?.Id is not null && playerMap.TryGetValue(s.PlayerOut.Id.Value.ToString(), out var outId))
                {
                    DbContext.MatchEvents.Add(new MatchEvent
                    {
                        MatchId = match.Id, PlayerId = outId,
                        EventType = MatchEventType.SubOut, Value = 1, Minute = FormatMinute(s.Minute, null)
                    });
                    eventCount++;
                }
            }

            await Task.Delay(6500, ct); // ~9 req/min, stay under 10/min limit
        }

        return eventCount;
    }

    private static string? FormatMinute(int? minute, int? extraTime) =>
        minute.HasValue ? extraTime > 0 ? $"{minute}+{extraTime}'" : $"{minute}'" : null;
}
