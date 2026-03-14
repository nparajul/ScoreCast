using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class JoinPredictionLeagueEndpoint : Endpoint<JoinPredictionLeagueRequest, ScoreCastResponse<PredictionLeagueResult>>
{
    public override void Configure()
    {
        Post("/leagues/join");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(JoinPredictionLeagueRequest request, CancellationToken ct)
    {
        var result = await new JoinPredictionLeagueCommand(request).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
