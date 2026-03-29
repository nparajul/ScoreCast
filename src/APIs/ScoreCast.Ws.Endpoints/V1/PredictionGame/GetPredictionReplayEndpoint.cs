using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class GetPredictionReplayEndpoint : Endpoint<GetPredictionReplayRequest, ScoreCastResponse<PredictionReplayResult>>
{
    public override void Configure()
    {
        Get("/replay/{MatchId}/{PredictionLeagueId}");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(GetPredictionReplayRequest request, CancellationToken ct)
    {
        var result = await new GetPredictionReplayQuery(request.MatchId, request.UserId!, request.PredictionLeagueId)
            .ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
