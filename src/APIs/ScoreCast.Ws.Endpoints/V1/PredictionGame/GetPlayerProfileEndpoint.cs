using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class GetPlayerProfileEndpoint : Endpoint<GetPlayerProfileRequest, ScoreCastResponse<PlayerProfileResult>>
{
    public override void Configure()
    {
        Get("/profile/{TargetUserId}/{PredictionLeagueId}");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(GetPlayerProfileRequest request, CancellationToken ct)
    {
        var result = await new GetPlayerProfileQuery(request.TargetUserId, request.PredictionLeagueId, request.UserId!).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
