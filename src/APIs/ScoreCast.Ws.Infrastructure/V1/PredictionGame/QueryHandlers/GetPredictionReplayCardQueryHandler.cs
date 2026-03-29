using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetPredictionReplayCardQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetPredictionReplayCardQuery, ScoreCastResponse<PredictionReplayCardResult>>
{
    public async Task<ScoreCastResponse<PredictionReplayCardResult>> ExecuteAsync(GetPredictionReplayCardQuery query, CancellationToken ct)
    {
        var match = await DbContext.Matches.AsNoTracking()
            .Where(m => m.Id == query.MatchId && m.Status == MatchStatus.Finished)
            .Select(m => new { m.HomeScore, m.AwayScore, Home = m.HomeTeam.ShortName ?? m.HomeTeam.Name, Away = m.AwayTeam.ShortName ?? m.AwayTeam.Name })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return ScoreCastResponse<PredictionReplayCardResult>.Error("Match not found.");

        var pred = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.MatchId == query.MatchId && p.UserId == query.UserId && p.PredictionType == PredictionType.Score && !p.IsDeleted)
            .Select(p => new { p.PredictedHomeScore, p.PredictedAwayScore, p.Outcome, DisplayName = p.User.DisplayName ?? "Player" })
            .FirstOrDefaultAsync(ct);

        if (pred is null)
            return ScoreCastResponse<PredictionReplayCardResult>.Error("Prediction not found.");

        var points = pred.Outcome is not null
            ? await DbContext.PredictionScoringRules.AsNoTracking()
                .Where(r => r.Outcome == pred.Outcome && r.PredictionType == PredictionType.Score && !r.IsDeleted)
                .Select(r => r.Points).FirstOrDefaultAsync(ct)
            : 0;

        var (label, color) = pred.Outcome switch
        {
            PredictionOutcome.ExactScore => ("EXACT SCORE 🎯", "#2E7D32"),
            PredictionOutcome.CorrectResultAndGoalDifference => ("CORRECT RESULT + GD", "#1565C0"),
            PredictionOutcome.CorrectResult => ("CORRECT RESULT", "#1565C0"),
            PredictionOutcome.CorrectGoalDifference => ("CORRECT GD", "#FF6B35"),
            _ => ("INCORRECT", "#C62828")
        };

        return ScoreCastResponse<PredictionReplayCardResult>.Ok(new PredictionReplayCardResult(
            pred.DisplayName, match.Home, match.Away,
            match.HomeScore ?? 0, match.AwayScore ?? 0,
            pred.PredictedHomeScore ?? 0, pred.PredictedAwayScore ?? 0,
            label, color, points));
    }
}
