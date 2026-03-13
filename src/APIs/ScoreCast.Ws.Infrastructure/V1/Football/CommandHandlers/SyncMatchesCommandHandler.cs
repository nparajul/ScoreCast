using System.Net.Http.Json;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Application.V1.Football.Commands;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Ws.Domain.V1.Enums;
using ScoreCast.Ws.Infrastructure.V1.Football.ExternalModels;

namespace ScoreCast.Ws.Infrastructure.V1.Football.CommandHandlers;

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

        var client = HttpClientFactory.CreateClient(nameof(ScoreCastHttpClient.FootballDataClient));
        var allMatches = new List<(Season Season, List<FootballDataMatch> Matches)>();

        foreach (var season in seasons)
        {
            var seasonYear = season.StartDate.Year;
            try
            {
                var apiResponse = await client.GetFromJsonAsync<FootballDataMatchesResponse>(
                    string.Format(FootballDataApi.Routes.Matches, command.Request.CompetitionCode, seasonYear), ct);

                if (apiResponse?.Matches is { Count: > 0 })
                    allMatches.Add((season, apiResponse.Matches));
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                break;
            }
            catch (Exception ex)
            {
                return ScoreCastResponse.Error($"Failed to fetch {seasonYear}: {ex.Message}");
            }
        }

        if (allMatches.Count == 0)
            return ScoreCastResponse.Error($"No matches returned for {competition.Name}");

        await using var transaction = await UnitOfWork.BeginTransactionAsync(ct);
        try
        {
            var teamCache = await DbContext.Teams
                .Where(t => t.ExternalId != null)
                .ToDictionaryAsync(t => t.ExternalId!, ct);

            var totalMatches = 0;
            foreach (var (season, matches) in allMatches)
                totalMatches += await UpsertMatchesForSeasonAsync(season, competition.Country, matches, teamCache, ct);

            await UnitOfWork.SaveChangesAsync(nameof(SyncMatchesCommand), ct);
            await transaction.CommitAsync(ct);
            return ScoreCastResponse.Ok($"Synced {totalMatches} matches across {allMatches.Count} seasons for {competition.Name}");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            return ScoreCastResponse.Error($"Failed to sync matches for {competition.Name}: {ex.InnerException?.Message ?? ex.Message}");
        }
    }

    private async Task<int> UpsertMatchesForSeasonAsync(
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

            var kickoff = DateTime.TryParse(apiMatch.UtcDate, out var dt)
                ? DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                : (DateTime?)null;

            var status = MapStatus(apiMatch.Status);

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
                    Status = status
                };
                DbContext.Matches.Add(match);
            }
            else
            {
                match.KickoffTime = kickoff;
                match.HomeScore = apiMatch.Score.FullTime?.Home;
                match.AwayScore = apiMatch.Score.FullTime?.Away;
                match.Status = status;
            }

            // update gameweek dates from match kickoffs
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

        // update gameweek statuses
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

    private static MatchStatus MapStatus(string apiStatus) => apiStatus switch
    {
        FootballDataApi.Status.Finished => MatchStatus.Finished,
        FootballDataApi.Status.InPlay or FootballDataApi.Status.Paused or FootballDataApi.Status.Live => MatchStatus.Live,
        FootballDataApi.Status.Postponed or FootballDataApi.Status.Suspended => MatchStatus.Postponed,
        FootballDataApi.Status.Cancelled => MatchStatus.Cancelled,
        _ => MatchStatus.Scheduled
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
