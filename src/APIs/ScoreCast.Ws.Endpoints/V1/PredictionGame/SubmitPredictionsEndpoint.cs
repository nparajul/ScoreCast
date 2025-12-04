using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class SubmitPredictionsEndpoint : Endpoint<SubmitPredictionsRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/predictions");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(SubmitPredictionsRequest request, CancellationToken ct)
    {
        var result = await new SubmitPredictionsCommand(request).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
