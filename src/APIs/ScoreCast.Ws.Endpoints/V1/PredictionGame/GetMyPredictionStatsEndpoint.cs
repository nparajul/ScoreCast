using ScoreCast.Models.V1.Requests;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class GetMyPredictionStatsEndpoint : Endpoint<ScoreCastRequest, ScoreCastResponse<MyPredictionStatsResult>>
{
    public override void Configure()
    {
        Get("/my-stats");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(ScoreCastRequest request, CancellationToken ct)
    {
        var result = await new GetMyPredictionStatsQuery(request.UserId!).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
