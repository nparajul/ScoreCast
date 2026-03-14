using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class GetMyPredictionsEndpoint : Endpoint<GetMyPredictionsRequest, ScoreCastResponse<List<MyPredictionResult>>>
{
    public override void Configure()
    {
        Get("/predictions/{SeasonId}/{GameweekId}");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(GetMyPredictionsRequest request, CancellationToken ct)
    {
        var result = await new GetMyPredictionsQuery(request.SeasonId, request.GameweekId, request.UserId!).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
