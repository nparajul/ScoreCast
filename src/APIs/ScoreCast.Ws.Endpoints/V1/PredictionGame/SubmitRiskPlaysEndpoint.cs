using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class SubmitRiskPlaysEndpoint : Endpoint<SubmitRiskPlaysRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/risk-plays");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(SubmitRiskPlaysRequest request, CancellationToken ct)
    {
        var result = await new SubmitRiskPlaysCommand(request).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
