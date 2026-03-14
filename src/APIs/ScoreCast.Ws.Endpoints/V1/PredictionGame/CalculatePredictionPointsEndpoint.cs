using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class CalculatePredictionPointsEndpoint : Endpoint<CalculatePredictionPointsRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/predictions/calculate");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(CalculatePredictionPointsRequest request, CancellationToken ct)
    {
        var result = await new CalculatePredictionPointsCommand(request).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
