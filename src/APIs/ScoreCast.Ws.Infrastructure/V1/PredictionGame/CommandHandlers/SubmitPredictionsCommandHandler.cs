using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;
using ScoreCast.Ws.Domain.V1.Entities.Football;

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

        var isMember = await DbContext.PredictionLeagueMembers
            .AnyAsync(m => m.PredictionLeagueId == request.PredictionLeagueId && m.UserId == user.Id, ct);

        if (!isMember)
            return ScoreCastResponse.Error("Not a member of this league");

        var matchIds = request.Predictions.Select(p => p.MatchId).ToList();

        var existing = await DbContext.Predictions
            .Where(p => p.PredictionLeagueId == request.PredictionLeagueId
                        && p.UserId == user.Id
                        && matchIds.Contains(p.MatchId))
            .ToDictionaryAsync(p => p.MatchId, ct);

        foreach (var entry in request.Predictions)
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
                    PredictionLeagueId = request.PredictionLeagueId,
                    UserId = user.Id,
                    MatchId = entry.MatchId,
                    PredictedHomeScore = entry.PredictedHomeScore,
                    PredictedAwayScore = entry.PredictedAwayScore
                });
            }
        }

        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(SubmitPredictionsCommand), ct);

        return ScoreCastResponse.Ok($"Saved {request.Predictions.Count} predictions");
    }
}
