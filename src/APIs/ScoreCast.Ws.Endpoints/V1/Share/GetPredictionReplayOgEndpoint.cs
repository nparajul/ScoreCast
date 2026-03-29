using Microsoft.AspNetCore.Http;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Share;

public sealed class GetPredictionReplayOgEndpoint : Endpoint<GetPredictionReplayCardRequest>
{
    public override void Configure()
    {
        Get("/share/replay/{MatchId}/{TargetUserId}/og");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetPredictionReplayCardRequest req, CancellationToken ct)
    {
        var result = await new GetPredictionReplayCardQuery(req.MatchId, req.TargetUserId).ExecuteAsync(ct);

        if (!result.Success || result.Data is null)
        {
            HttpContext.Response.Redirect($"https://scorecast.uk/match/{req.MatchId}");
            return;
        }

        var d = result.Data;
        var e = (Func<string, string>)(s => s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;"));
        var title = $"{d.DisplayName} predicted {d.HomeTeam} {d.PredictedHome}-{d.PredictedAway} {d.AwayTeam}";
        var desc = $"Result: {d.HomeTeam} {d.HomeScore}-{d.AwayScore} {d.AwayTeam} — {d.OutcomeLabel}";
        var img = $"https://scorecast.uk/api/v1/share/replay/{req.MatchId}/{req.TargetUserId}";
        var page = $"https://scorecast.uk/replay/{req.MatchId}/0";

        HttpContext.Response.ContentType = "text/html";
        await HttpContext.Response.WriteAsync($"""
            <!DOCTYPE html><html><head>
            <meta charset="utf-8"/><title>{e(title)} | ScoreCast</title>
            <meta property="og:title" content="{e(title)}"/>
            <meta property="og:description" content="{e(desc)}"/>
            <meta property="og:image" content="{img}"/>
            <meta property="og:image:width" content="1200"/><meta property="og:image:height" content="630"/>
            <meta property="og:url" content="{page}"/><meta property="og:type" content="website"/><meta property="og:site_name" content="ScoreCast"/>
            <meta name="twitter:card" content="summary_large_image"/>
            <meta name="twitter:title" content="{e(title)}"/><meta name="twitter:description" content="{e(desc)}"/><meta name="twitter:image" content="{img}"/>
            <meta http-equiv="refresh" content="0;url={page}"/>
            </head><body><p>Redirecting to <a href="{page}">ScoreCast</a>...</p></body></html>
            """, ct);
    }
}
