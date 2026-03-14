using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class CreatePredictionLeagueEndpoint : Endpoint<CreatePredictionLeagueRequest, ScoreCastResponse<PredictionLeagueResult>>
{
    public override void Configure()
    {
        Post("/leagues");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(CreatePredictionLeagueRequest request, CancellationToken ct)
    {
        var result = await new CreatePredictionLeagueCommand(request).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
