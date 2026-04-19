using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Share;

public sealed class GetGameweekReplayEndpoint : Endpoint<GetGameweekReplayRequest, ScoreCastResponse<GameweekReplayResult>>
{
    public override void Configure()
    {
        Get("/gw-replay/{SeasonId}/{GameweekNumber}/{TargetUserId}");
        Group<ShareGroup>();
    }

    public override async Task HandleAsync(GetGameweekReplayRequest req, CancellationToken ct)
    {
        var result = await new GetGameweekReplayQuery(req.SeasonId, req.GameweekNumber, req.TargetUserId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
