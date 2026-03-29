using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Endpoints.V1.Share;

public sealed class GetPredictionReplayOgEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/share/replay/{matchId}/{userId}/og");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var matchId = Route<long>("matchId");
        var userId = Route<long>("userId");
        var db = Resolve<IScoreCastDbContext>();

        var match = await db.Matches.AsNoTracking()
            .Where(m => m.Id == matchId && m.Status == MatchStatus.Finished)
            .Select(m => new { m.HomeScore, m.AwayScore, Home = m.HomeTeam.ShortName ?? m.HomeTeam.Name, Away = m.AwayTeam.ShortName ?? m.AwayTeam.Name })
            .FirstOrDefaultAsync(ct);

        var pred = match is null ? null : await db.Predictions.AsNoTracking()
            .Where(p => p.MatchId == matchId && p.UserId == userId && p.PredictionType == PredictionType.Score && !p.IsDeleted)
            .Select(p => new { p.PredictedHomeScore, p.PredictedAwayScore, p.Outcome, DisplayName = p.User.DisplayName ?? "Player" })
            .FirstOrDefaultAsync(ct);

        if (match is null || pred is null) { HttpContext.Response.Redirect($"https://scorecast.uk/match/{matchId}"); return; }

        var e = (Func<string, string>)(s => s.Replace("&", "&amp;").Replace("\"", "&quot;").Replace("<", "&lt;"));
        var title = $"{pred.DisplayName} predicted {match.Home} {pred.PredictedHomeScore}-{pred.PredictedAwayScore} {match.Away}";
        var desc = $"Result: {match.Home} {match.HomeScore}-{match.AwayScore} {match.Away} — {pred.Outcome?.ToString() ?? "Pending"}";
        var img = $"https://scorecast.uk/api/v1/share/replay/{matchId}/{userId}";
        var page = $"https://scorecast.uk/replay/{matchId}/0";

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
