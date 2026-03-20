using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.CommandHandlers;

internal sealed record CalculateOutcomesCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<CalculateOutcomesCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(CalculateOutcomesCommand command, CancellationToken ct)
    {
        var predictions = await DbContext.Predictions
            .Include(p => p.Match)
            .Where(p => p.SeasonId == command.Request.SeasonId
                        && p.PredictionType == PredictionType.Score
                        && p.Outcome == null
                        && p.Match!.Status == MatchStatus.Finished
                        && p.Match.HomeScore != null && p.Match.AwayScore != null)
            .ToListAsync(ct);

        foreach (var prediction in predictions)
        {
            prediction.Outcome = DetermineOutcome(
                prediction.PredictedHomeScore!.Value, prediction.PredictedAwayScore!.Value,
                prediction.Match!.HomeScore!.Value, prediction.Match.AwayScore!.Value);
        }

        if (predictions.Count > 0)
        {
            var scoringRules = await DbContext.PredictionScoringRules
                .AsNoTracking()
                .Where(r => r.PredictionType == PredictionType.Score && r.StageType == null)
                .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

            var pointsByUser = predictions
                .GroupBy(p => p.UserId)
                .ToDictionary(g => g.Key, g => g.Sum(p => scoringRules.GetValueOrDefault(p.Outcome!.Value)));

            var userIds = pointsByUser.Keys.ToList();
            var users = await DbContext.UserMasters
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync(ct);

            foreach (var user in users)
                user.TotalPoints += pointsByUser[user.Id];

            // Recalculate streaks for affected users
            foreach (var user in users)
            {
                var allOutcomes = await DbContext.Predictions
                    .Include(p => p.Match)
                    .Where(p => p.UserId == user.Id && p.Outcome != null && p.Match!.KickoffTime != null)
                    .OrderBy(p => p.Match!.KickoffTime)
                    .Select(p => p.Outcome!.Value)
                    .ToListAsync(ct);

                var current = 0;
                var longest = 0;
                var streak = 0;
                foreach (var outcome in allOutcomes)
                {
                    if (outcome != PredictionOutcome.Incorrect)
                        streak++;
                    else
                        streak = 0;
                    if (streak > longest) longest = streak;
                }
                current = streak;
                user.CurrentStreak = current;
                user.LongestStreak = longest;
            }
        }

        await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(CalculateOutcomesCommand), ct);

        return ScoreCastResponse.Ok($"Updated {predictions.Count} prediction outcomes");
    }

    private static PredictionOutcome DetermineOutcome(int predHome, int predAway, int actualHome, int actualAway)
    {
        if (predHome == actualHome && predAway == actualAway)
            return PredictionOutcome.ExactScore;

        var predDiff = predHome - predAway;
        var actualDiff = actualHome - actualAway;
        var predResult = Math.Sign(predDiff);
        var actualResult = Math.Sign(actualDiff);

        if (predResult == actualResult && predDiff == actualDiff)
            return PredictionOutcome.CorrectResultAndGoalDifference;

        if (predResult == actualResult)
            return PredictionOutcome.CorrectResult;

        if (predDiff == actualDiff)
            return PredictionOutcome.CorrectGoalDifference;

        return PredictionOutcome.Incorrect;
    }
}
