using Microsoft.AspNetCore.Http;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Share;

public sealed class GetPredictionReplayOgEndpoint : Endpoint<GetPredictionReplayCardRequest>
{
    public override void Configure()
    {
        Get("/replay/{MatchId}/{TargetUserId}/og");
        Group<ShareGroup>();
    }

    public override async Task HandleAsync(GetPredictionReplayCardRequest req, CancellationToken ct)
    {
        var baseUrl = $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";
        var result = await new GetPredictionReplayCardQuery(req.MatchId, req.TargetUserId, baseUrl).ExecuteAsync(ct);
        if (!result.Success || result.Data is null) { HttpContext.Response.Redirect($"/matches/{req.MatchId}"); return; }

        HttpContext.Response.ContentType = "text/html";
        await HttpContext.Response.WriteAsync(result.Data.OgHtml, ct);
    }
}
