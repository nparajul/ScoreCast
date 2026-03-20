using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class GetMyRiskPlaysEndpoint : Endpoint<GetMyRiskPlaysRequest, ScoreCastResponse<List<RiskPlayResult>>>
{
    public override void Configure()
    {
        Get("/risk-plays/{SeasonId}/{GameweekId}");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(GetMyRiskPlaysRequest request, CancellationToken ct)
    {
        var result = await new GetMyRiskPlaysQuery(request.SeasonId, request.GameweekId, request.UserId!).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
