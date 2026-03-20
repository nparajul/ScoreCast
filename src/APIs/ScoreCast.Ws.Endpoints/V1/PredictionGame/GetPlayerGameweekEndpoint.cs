using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class GetPlayerGameweekEndpoint : Endpoint<GetPlayerGameweekRequest, ScoreCastResponse<PlayerGameweekResult>>
{
    public override void Configure()
    {
        Get("/profile/{TargetUserId}/{PredictionLeagueId}/{SeasonId}/{GameweekId}");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(GetPlayerGameweekRequest request, CancellationToken ct)
    {
        var result = await new GetPlayerGameweekQuery(
            request.TargetUserId, request.SeasonId, request.GameweekId,
            request.PredictionLeagueId, request.UserId!).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
