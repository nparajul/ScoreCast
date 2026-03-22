using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetGlobalLeaderboardQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetGlobalLeaderboardQuery, ScoreCastResponse<GlobalLeaderboardResult>>
{
    public async Task<ScoreCastResponse<GlobalLeaderboardResult>> ExecuteAsync(GetGlobalLeaderboardQuery query, CancellationToken ct)
    {
        var season = await DbContext.Seasons
            .FirstOrDefaultAsync(s => s.Competition.Code == query.CompetitionCode && s.IsCurrent, ct);
        if (season is null)
            return ScoreCastResponse<GlobalLeaderboardResult>.Error("No current season.");

        var allPreds = await DbContext.Predictions
            .Where(p => p.SeasonId == season.Id && p.PredictionType == PredictionType.Score && p.Outcome != null)
            .Select(p => new { p.UserId, Outcome = p.Outcome!.Value })
            .ToListAsync(ct);

        var scoringRules = await DbContext.PredictionScoringRules
            .Where(r => r.PredictionType == PredictionType.Score)
            .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

        var userScores = allPreds.GroupBy(p => p.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Points = g.Sum(p => scoringRules.GetValueOrDefault(p.Outcome, 0)),
                Exact = g.Count(p => p.Outcome == PredictionOutcome.ExactScore),
                Total = g.Count()
            })
            .OrderByDescending(u => u.Points)
            .ToList();

        var userIds = userScores.Select(u => u.UserId).ToList();
        var userNames = await DbContext.UserMasters
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName ?? "Anonymous", ct);

        var entries = userScores.Select((u, i) => new GlobalLeaderboardEntry(
            i + 1, userNames.GetValueOrDefault(u.UserId, "Anonymous"), u.Points, u.Exact, u.Total)).ToList();

        return ScoreCastResponse<GlobalLeaderboardResult>.Ok(new GlobalLeaderboardResult(entries));
    }
}
