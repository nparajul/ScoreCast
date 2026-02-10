using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.CommandHandlers;

internal sealed record SubmitPredictionsCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<SubmitPredictionsCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(SubmitPredictionsCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var user = await DbContext.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, ct);

        if (user is null)
            return ScoreCastResponse.Error("User not found");

        var matchIds = request.Predictions.Select(p => p.MatchId).ToList();

        // Check kickoff times — reject matches that have already started
        var now = ScoreCastDateTime.Now.Value;
        var matches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => matchIds.Contains(m.Id))
            .Select(m => new { m.Id, m.KickoffTime, m.Status })
            .ToListAsync(ct);

        var lockedMatchIds = matches
            .Where(m => m.Status != MatchStatus.Scheduled
                        || (m.KickoffTime.HasValue && m.KickoffTime.Value <= now))
            .Select(m => m.Id)
            .ToHashSet();

        var validPredictions = request.Predictions.Where(p => !lockedMatchIds.Contains(p.MatchId)).ToList();
        var skippedCount = request.Predictions.Count - validPredictions.Count;

        if (validPredictions.Count == 0 && skippedCount > 0)
            return ScoreCastResponse.Error("All matches have already kicked off — predictions cannot be saved");

        var validMatchIds = validPredictions.Select(p => p.MatchId).ToList();
        var existing = await DbContext.Predictions
            .Where(p => p.SeasonId == request.SeasonId
                        && p.UserId == user.Id
                        && p.MatchId != null
                        && validMatchIds.Contains(p.MatchId.Value))
            .ToDictionaryAsync(p => p.MatchId!.Value, ct);

        foreach (var entry in validPredictions)
        {
            if (existing.TryGetValue(entry.MatchId, out var prediction))
            {
                prediction.PredictedHomeScore = entry.PredictedHomeScore;
                prediction.PredictedAwayScore = entry.PredictedAwayScore;
            }
            else
            {
                DbContext.Predictions.Add(new Domain.V1.Entities.Football.Prediction
                {
                    SeasonId = request.SeasonId,
                    UserId = user.Id,
                    MatchId = entry.MatchId,
                    PredictedHomeScore = entry.PredictedHomeScore,
                    PredictedAwayScore = entry.PredictedAwayScore
                });
            }
        }

        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(SubmitPredictionsCommand), ct);

        if (skippedCount > 0)
            return ScoreCastResponse.Ok($"Saved {validPredictions.Count} predictions. {skippedCount} skipped — matches already kicked off.");

        return ScoreCastResponse.Ok($"Saved {validPredictions.Count} predictions");
    }
}
