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
        var finishedMatches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == command.SeasonId
                        && m.Status == MatchStatus.Finished
                        && m.HomeScore != null && m.AwayScore != null)
            .Select(m => new { m.Id, m.HomeScore, m.AwayScore })
            .ToDictionaryAsync(m => m.Id, ct);

        var predictions = await DbContext.Predictions
            .Where(p => p.SeasonId == command.SeasonId
                        && p.Outcome == null
                        && finishedMatches.Keys.Contains(p.MatchId))
            .ToListAsync(ct);

        foreach (var prediction in predictions)
        {
            var match = finishedMatches[prediction.MatchId];
            prediction.Outcome = DetermineOutcome(
                prediction.PredictedHomeScore, prediction.PredictedAwayScore,
                match.HomeScore!.Value, match.AwayScore!.Value);
        }

        await UnitOfWork.SaveChangesAsync(nameof(CalculateOutcomesCommand), ct);

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
