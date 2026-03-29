using System.Text;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Endpoints.V1.Share;

public sealed class GetPredictionReplayCardEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("/share/replay/{matchId}/{userId}");
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

        if (match is null || pred is null) { HttpContext.Response.StatusCode = 404; return; }

        var points = pred.Outcome is not null
            ? await db.PredictionScoringRules.AsNoTracking()
                .Where(r => r.Outcome == pred.Outcome && r.PredictionType == PredictionType.Score && !r.IsDeleted)
                .Select(r => r.Points).FirstOrDefaultAsync(ct)
            : 0;

        var (outcomeLabel, outcomeColor) = pred.Outcome switch
        {
            PredictionOutcome.ExactScore => ("EXACT SCORE 🎯", "#2E7D32"),
            PredictionOutcome.CorrectResultAndGoalDifference => ("CORRECT RESULT + GD", "#1565C0"),
            PredictionOutcome.CorrectResult => ("CORRECT RESULT", "#1565C0"),
            PredictionOutcome.CorrectGoalDifference => ("CORRECT GD", "#FF6B35"),
            _ => ("INCORRECT", "#C62828")
        };

        var svg = BuildSvg(pred.DisplayName, match.Home, match.Away, match.HomeScore ?? 0, match.AwayScore ?? 0,
            pred.PredictedHomeScore ?? 0, pred.PredictedAwayScore ?? 0, outcomeLabel, outcomeColor, points);

        HttpContext.Response.ContentType = "image/svg+xml";
        await HttpContext.Response.WriteAsync(svg, ct);
    }

    private static string BuildSvg(string displayName, string home, string away, int hs, int @as, int ph, int pa, string label, string color, int pts)
    {
        var e = (Func<string, string>)(s => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;"));
        var bw = label.Length * 14 + 40;
        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
            <defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0%" stop-color="#0A1929"/><stop offset="100%" stop-color="#37003C"/></linearGradient></defs>
            <rect width="1200" height="630" fill="url(#bg)"/>
            <text x="600" y="60" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="20" font-weight="700" opacity="0.5">SCORECAST</text>
            <text x="600" y="110" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="16" font-weight="600" opacity="0.4" letter-spacing="0.15em">FULL TIME</text>
            <text x="340" y="185" text-anchor="end" fill="white" font-family="Inter,system-ui,sans-serif" font-size="36" font-weight="700">{e(home)}</text>
            <text x="600" y="190" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="64" font-weight="800">{hs} – {@as}</text>
            <text x="860" y="185" text-anchor="start" fill="white" font-family="Inter,system-ui,sans-serif" font-size="36" font-weight="700">{e(away)}</text>
            <line x1="300" y1="230" x2="900" y2="230" stroke="white" stroke-opacity="0.15" stroke-width="1"/>
            <text x="600" y="280" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="22" font-weight="600" opacity="0.7">{e(displayName)}'s Prediction</text>
            <text x="600" y="350" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="72" font-weight="800">{ph} – {pa}</text>
            <rect x="{600 - bw / 2}" y="385" width="{bw}" height="40" rx="20" fill="{color}"/>
            <text x="600" y="412" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="16" font-weight="700">{label} · {pts} pts</text>
            <text x="600" y="580" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="18" font-weight="600" opacity="0.3">scorecast.uk</text>
            </svg>
            """;
    }
}
