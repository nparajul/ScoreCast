using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetMyPredictionsQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetMyPredictionsQuery, ScoreCastResponse<List<MyPredictionResult>>>
{
    public async Task<ScoreCastResponse<List<MyPredictionResult>>> ExecuteAsync(GetMyPredictionsQuery query, CancellationToken ct)
    {
        var user = await DbContext.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == query.UserId, ct);

        if (user is null)
            return ScoreCastResponse<List<MyPredictionResult>>.Error("User not found");

        var matchIds = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.GameweekId == query.GameweekId)
            .Select(m => m.Id)
            .ToListAsync(ct);

        var predictions = await DbContext.Predictions
            .AsNoTracking()
            .Where(p => p.SeasonId == query.SeasonId
                        && p.UserId == user.Id
                        && matchIds.Contains(p.MatchId))
            .Select(p => new MyPredictionResult(p.MatchId, p.PredictedHomeScore, p.PredictedAwayScore, p.Outcome))
            .ToListAsync(ct);

        return ScoreCastResponse<List<MyPredictionResult>>.Ok(predictions);
    }
}
