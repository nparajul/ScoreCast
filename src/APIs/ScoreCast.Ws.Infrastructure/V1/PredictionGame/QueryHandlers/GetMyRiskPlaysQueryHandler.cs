using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetMyRiskPlaysQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetMyRiskPlaysQuery, ScoreCastResponse<List<RiskPlayResult>>>
{
    public async Task<ScoreCastResponse<List<RiskPlayResult>>> ExecuteAsync(GetMyRiskPlaysQuery query, CancellationToken ct)
    {
        var user = await DbContext.UserMasters.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == query.UserId, ct);
        if (user is null) return ScoreCastResponse<List<RiskPlayResult>>.Ok([]);

        var plays = await DbContext.RiskPlays
            .AsNoTracking()
            .Where(r => r.SeasonId == query.SeasonId && r.GameweekId == query.GameweekId
                        && r.UserId == user.Id && !r.IsDeleted)
            .Select(r => new RiskPlayResult(r.Id, r.MatchId, r.RiskType, r.Selection,
                r.BonusPoints, r.IsResolved, r.IsWon))
            .ToListAsync(ct);

        return ScoreCastResponse<List<RiskPlayResult>>.Ok(plays);
    }
}
