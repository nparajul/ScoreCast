using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class CalculateOutcomesEndpoint : Endpoint<CalculateOutcomesRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/calculate-outcomes");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(CalculateOutcomesRequest request, CancellationToken ct)
    {
        var result = await new CalculateOutcomesCommand(request).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
