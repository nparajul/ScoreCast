using Microsoft.AspNetCore.Http;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Share;

public sealed class GetPredictionReplayCardEndpoint : Endpoint<GetPredictionReplayCardRequest>
{
    public override void Configure()
    {
        Get("/replay/{MatchId}/{TargetUserId}");
        Group<ShareGroup>();
    }

    public override async Task HandleAsync(GetPredictionReplayCardRequest req, CancellationToken ct)
    {
        var result = await new GetPredictionReplayCardQuery(req.MatchId, req.TargetUserId).ExecuteAsync(ct);
        if (!result.Success || result.Data is null) { HttpContext.Response.StatusCode = 404; return; }

        HttpContext.Response.ContentType = "image/svg+xml";
        await HttpContext.Response.WriteAsync(result.Data.Svg, ct);
    }
}
