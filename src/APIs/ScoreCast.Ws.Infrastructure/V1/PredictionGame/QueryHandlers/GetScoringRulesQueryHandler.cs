using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetScoringRulesQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetScoringRulesQuery, ScoreCastResponse<List<ScoringRuleResult>>>
{
    public async Task<ScoreCastResponse<List<ScoringRuleResult>>> ExecuteAsync(GetScoringRulesQuery query, CancellationToken ct)
    {
        var rules = await DbContext.PredictionScoringRules
            .AsNoTracking()
            .OrderBy(r => r.DisplayOrder)
            .Select(r => new ScoringRuleResult(r.Outcome.ToString(), r.Points, r.Description, r.DisplayOrder))
            .ToListAsync(ct);

        return ScoreCastResponse<List<ScoringRuleResult>>.Ok(rules);
    }
}
