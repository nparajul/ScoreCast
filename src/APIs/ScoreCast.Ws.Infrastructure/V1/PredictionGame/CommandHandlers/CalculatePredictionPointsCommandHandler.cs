using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.CommandHandlers;

internal sealed record CalculatePredictionPointsCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<CalculatePredictionPointsCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(CalculatePredictionPointsCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var scoringRules = await DbContext.PredictionScoringRules
            .AsNoTracking()
            .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

        var finishedMatches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == request.SeasonId && m.Status == MatchStatus.Finished)
            .Select(m => new { m.Id, m.HomeScore, m.AwayScore })
            .ToDictionaryAsync(m => m.Id, ct);

        var predictions = await DbContext.Predictions
            .Where(p => finishedMatches.Keys.Contains(p.MatchId))
            .ToListAsync(ct);

        var updated = 0;
        foreach (var prediction in predictions)
        {
            var match = finishedMatches[prediction.MatchId];
            if (match.HomeScore is null || match.AwayScore is null) continue;

            var outcome = DetermineOutcome(
                prediction.PredictedHomeScore, prediction.PredictedAwayScore,
                match.HomeScore.Value, match.AwayScore.Value);

            var points = scoringRules.GetValueOrDefault(outcome, 0);

            if (prediction.PointsAwarded != points)
            {
                prediction.PointsAwarded = points;
                updated++;
            }
        }

        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(CalculatePredictionPointsCommand), ct);

        return ScoreCastResponse.Ok($"Updated {updated} predictions");
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
