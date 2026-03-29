using Microsoft.AspNetCore.Http;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Share;

public sealed class GetPredictionReplayCardEndpoint : Endpoint<GetPredictionReplayCardRequest>
{
    public override void Configure()
    {
        Get("/share/replay/{MatchId}/{TargetUserId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetPredictionReplayCardRequest req, CancellationToken ct)
    {
        var result = await new GetPredictionReplayCardQuery(req.MatchId, req.TargetUserId).ExecuteAsync(ct);
        if (!result.Success || result.Data is null) { HttpContext.Response.StatusCode = 404; return; }

        var d = result.Data;
        var e = (Func<string, string>)(s => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
        var bw = d.OutcomeLabel.Length * 14 + 40;

        var svg = $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
            <defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0%" stop-color="#0A1929"/><stop offset="100%" stop-color="#37003C"/></linearGradient></defs>
            <rect width="1200" height="630" fill="url(#bg)"/>
            <text x="600" y="60" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="20" font-weight="700" opacity="0.5">SCORECAST</text>
            <text x="600" y="110" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="16" font-weight="600" opacity="0.4" letter-spacing="0.15em">FULL TIME</text>
            <text x="340" y="185" text-anchor="end" fill="white" font-family="Inter,system-ui,sans-serif" font-size="36" font-weight="700">{e(d.HomeTeam)}</text>
            <text x="600" y="190" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="64" font-weight="800">{d.HomeScore} – {d.AwayScore}</text>
            <text x="860" y="185" text-anchor="start" fill="white" font-family="Inter,system-ui,sans-serif" font-size="36" font-weight="700">{e(d.AwayTeam)}</text>
            <line x1="300" y1="230" x2="900" y2="230" stroke="white" stroke-opacity="0.15" stroke-width="1"/>
            <text x="600" y="280" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="22" font-weight="600" opacity="0.7">{e(d.DisplayName)}'s Prediction</text>
            <text x="600" y="350" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="72" font-weight="800">{d.PredictedHome} – {d.PredictedAway}</text>
            <rect x="{600 - bw / 2}" y="385" width="{bw}" height="40" rx="20" fill="{d.OutcomeColor}"/>
            <text x="600" y="412" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="16" font-weight="700">{d.OutcomeLabel} · {d.Points} pts</text>
            <text x="600" y="580" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="18" font-weight="600" opacity="0.3">scorecast.uk</text>
            </svg>
            """;

        HttpContext.Response.ContentType = "image/svg+xml";
        await HttpContext.Response.WriteAsync(svg, ct);
    }
}
