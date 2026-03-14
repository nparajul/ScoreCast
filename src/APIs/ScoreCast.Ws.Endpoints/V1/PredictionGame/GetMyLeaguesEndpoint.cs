using ScoreCast.Models.V1.Requests;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class GetMyLeaguesEndpoint : Endpoint<ScoreCastRequest, ScoreCastResponse<List<PredictionLeagueResult>>>
{
    public override void Configure()
    {
        Get("/leagues/mine");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(ScoreCastRequest request, CancellationToken ct)
    {
        var result = await new GetMyLeaguesQuery(request).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
