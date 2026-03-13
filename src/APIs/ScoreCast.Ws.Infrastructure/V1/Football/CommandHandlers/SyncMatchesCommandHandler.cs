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
        var totalMatches = 0;
        var seasonsSynced = 0;
        var errors = new List<string>();

        foreach (var season in seasons)
        {
            var seasonYear = season.StartDate.Year;
            FootballDataMatchesResponse? apiResponse;
            try
            {
                apiResponse = await client.GetFromJsonAsync<FootballDataMatchesResponse>(
                    string.Format(FootballDataApi.Routes.Matches, command.Request.CompetitionCode, seasonYear), ct);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                break; // hit the free-tier limit, stop trying older seasons
            }
            catch (Exception ex)
            {
                errors.Add($"{seasonYear}: {ex.Message}");
                continue;
            }

            if (apiResponse?.Matches is null or { Count: 0 })
                continue;

            await using var transaction = await UnitOfWork.BeginTransactionAsync(ct);
            try
            {
                var matchCount = await UpsertMatchesForSeasonAsync(season, apiResponse.Matches, ct);
                await UnitOfWork.SaveChangesAsync(nameof(SyncMatchesCommand), ct);
                await transaction.CommitAsync(ct);
                totalMatches += matchCount;
                seasonsSynced++;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(ct);
                errors.Add($"{seasonYear}: {ex.Message}");
            }
        }

        var msg = $"Synced {totalMatches} matches across {seasonsSynced} seasons for {competition.Name}";
        if (errors.Count > 0)
            msg += $". Errors: {string.Join("; ", errors)}";

        return totalMatches > 0 || seasonsSynced > 0
            ? ScoreCastResponse.Ok(msg)
            : ScoreCastResponse.Error(msg);
    }

    private async Task<int> UpsertMatchesForSeasonAsync(
        Season season, List<FootballDataMatch> apiMatches, CancellationToken ct)
    {
        var gameweekCache = new Dictionary<int, Gameweek>();
        var existingGameweeks = await DbContext.Gameweeks
            .Where(g => g.SeasonId == season.Id)
            .ToDictionaryAsync(g => g.Number, ct);

        foreach (var kvp in existingGameweeks)
            gameweekCache[kvp.Key] = kvp.Value;

        // pre-load team external_id → id map
        var teamMap = await DbContext.Teams
            .Where(t => t.ExternalId != null)
            .ToDictionaryAsync(t => t.ExternalId!, t => t.Id, ct);

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

            var homeExtId = apiMatch.HomeTeam.Id.ToString();
            var awayExtId = apiMatch.AwayTeam.Id.ToString();

            if (!teamMap.TryGetValue(homeExtId, out var homeTeamId) ||
                !teamMap.TryGetValue(awayExtId, out var awayTeamId))
                continue; // skip if teams not synced yet

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
                    GameweekId = gameweek.Id,
                    HomeTeamId = homeTeamId,
                    AwayTeamId = awayTeamId,
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
}
