using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Share;

public sealed class GetPublicPredictionReplayEndpoint : Endpoint<GetPredictionReplayCardRequest, ScoreCastResponse<PredictionReplayResult>>
{
    public override void Configure()
    {
        Get("/replay/{MatchId}/{TargetUserId}/view");
        Group<ShareGroup>();
    }

    public override async Task HandleAsync(GetPredictionReplayCardRequest req, CancellationToken ct)
    {
        var result = await new GetPublicPredictionReplayQuery(req.MatchId, req.TargetUserId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
