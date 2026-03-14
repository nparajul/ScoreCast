using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
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

        var seasons = await DbContext.Seasons
            .Where(s => s.CompetitionId == competition.Id)
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

            if (isPremierLeague)
                totalMatches = await SyncFromPulseAsync(seasons, competition.Country, teamCache, ct);

            // Fallback to football-data.org if Pulse returned nothing, or for non-PL
            if (totalMatches == 0)
                totalMatches = await SyncFromFootballDataAsync(command.Request.CompetitionCode, seasons, competition.Country, teamCache, ct);

            await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(SyncMatchesCommand), ct);
            await transaction.CommitAsync(ct);
            return ScoreCastResponse.Ok($"Synced {totalMatches} matches across {seasons.Count} seasons for {competition.Name}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return ScoreCastResponse.Error($"Failed to sync matches for {competition.Name}: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private async Task<int> SyncFromPulseAsync(
        List<Season> seasons, Country country, Dictionary<string, Team> teamCache, CancellationToken ct)
    {
        var pulseClient = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.PulseClient));

        // Build Pulse team ID → our Team lookup
        var pulseTeamMap = await DbContext.ExternalMappings
            .Where(m => m.Source == ExternalSource.Pulse && m.EntityType == EntityType.Team)
            .ToDictionaryAsync(m => m.ExternalCode, m => m.EntityId, ct);

        var teamById = await DbContext.Teams.ToDictionaryAsync(t => t.Id, ct);

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
            catch (Exception)
            {
                continue;
            }

            if (response?.Content is not { Count: > 0 }) continue;

            totalMatches += await UpsertPulseMatchesAsync(season, country, response.Content, pulseTeamMap, teamById, teamCache, ct);
        }

        return totalMatches;
    }

    private async Task<int> UpsertPulseMatchesAsync(
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
        foreach (var pf in fixtures)
        {
            var pulseFixtureId = ((int)pf.Id).ToString();
            var matchday = pf.Gameweek?.Gameweek ?? 1;

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
                ? DateTimeOffset.FromUnixTimeMilliseconds(pf.Kickoff.Millis.Value).UtcDateTime
                : (DateTime?)null;

            var status = MapPulseStatus(pf.Status);
            var minute = FormatPulseMinute(pf);
            var referee = pf.MatchOfficials?.FirstOrDefault(o => o.Role == "REFEREE")?.Name?.Display;

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
                    HomeScore = pf.Teams[0].Score,
                    AwayScore = pf.Teams[1].Score,
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
                match.HomeScore = pf.Teams[0].Score;
                match.AwayScore = pf.Teams[1].Score;
                match.Status = status;
                match.Venue = homeTeam.Venue;
                match.Referee = referee;
                match.Minute = minute;
            }

            // Ensure Pulse external mapping exists
            if (!existingPulseMappings.ContainsKey(pulseFixtureId))
            {
                DbContext.ExternalMappings.Add(new ExternalMapping
                {
                    EntityType = EntityType.Match,
                    EntityId = match.Id,
                    Source = ExternalSource.Pulse,
                    ExternalCode = pulseFixtureId
                });
                existingPulseMappings[pulseFixtureId] = match.Id;
            }

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

        return count;
    }

    private async Task<int> SyncFromFootballDataAsync(
        string competitionCode, List<Season> seasons, Country country,
        Dictionary<string, Team> teamCache, CancellationToken ct)
    {
        var client = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FootballDataClient));
        var totalMatches = 0;

        foreach (var season in seasons)
        {
            var seasonYear = season.StartDate.Year;
            try
            {
                var apiResponse = await client.GetFromJsonAsync<FootballDataMatchesResponse>(
                    string.Format(FootballDataApi.Routes.Matches, competitionCode, seasonYear), ct);

                if (apiResponse?.Matches is { Count: > 0 })
                    totalMatches += await UpsertFdMatchesForSeasonAsync(season, country, apiResponse.Matches, teamCache, ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                break;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to fetch {seasonYear}: {ex.Message}", ex);
            }
        }

        return totalMatches;
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

        var count = 0;
        foreach (var apiMatch in apiMatches)
        {
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
            var match = await DbContext.Matches
                .FirstOrDefaultAsync(m => m.ExternalId == externalId, ct);

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
        var externalId = apiTeam.Id.ToString();
        if (teamCache.TryGetValue(externalId, out var team))
            return team;

        team = new Team
        {
            Name = apiTeam.Name,
            ShortName = apiTeam.ShortName,
            LogoUrl = apiTeam.Crest,
            ExternalId = externalId,
            Country = country
        };
        DbContext.Teams.Add(team);
        teamCache[externalId] = team;
        return team;
    }
}
