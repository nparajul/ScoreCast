using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class GetScoringRulesEndpoint : EndpointWithoutRequest<ScoreCastResponse<List<ScoringRuleResult>>>
{
    public override void Configure()
    {
        Get("/scoring-rules");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await new GetScoringRulesQuery().ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
