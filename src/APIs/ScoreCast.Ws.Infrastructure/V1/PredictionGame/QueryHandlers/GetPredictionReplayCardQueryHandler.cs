using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetPredictionReplayCardQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetPredictionReplayCardQuery, ScoreCastResponse<PredictionReplayCardResult>>
{
    public async Task<ScoreCastResponse<PredictionReplayCardResult>> ExecuteAsync(GetPredictionReplayCardQuery query, CancellationToken ct)
    {
        var match = await DbContext.Matches.AsNoTracking()
            .Where(m => m.Id == query.MatchId && m.Status == MatchStatus.Finished)
            .Select(m => new { m.HomeScore, m.AwayScore, Home = m.HomeTeam.ShortName ?? m.HomeTeam.Name, Away = m.AwayTeam.ShortName ?? m.AwayTeam.Name })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return ScoreCastResponse<PredictionReplayCardResult>.Error("Match not found.");

        var pred = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.MatchId == query.MatchId && p.UserId == query.UserId && p.PredictionType == PredictionType.Score && !p.IsDeleted)
            .Select(p => new { p.PredictedHomeScore, p.PredictedAwayScore, p.Outcome, DisplayName = p.User.DisplayName ?? "Player" })
            .FirstOrDefaultAsync(ct);

        if (pred is null)
            return ScoreCastResponse<PredictionReplayCardResult>.Error("Prediction not found.");

        var points = pred.Outcome is not null
            ? await DbContext.PredictionScoringRules.AsNoTracking()
                .Where(r => r.Outcome == pred.Outcome && r.PredictionType == PredictionType.Score && !r.IsDeleted)
                .Select(r => r.Points).FirstOrDefaultAsync(ct)
            : 0;

        var (label, color) = pred.Outcome switch
        {
            PredictionOutcome.ExactScore => ("EXACT SCORE 🎯", "#2E7D32"),
            PredictionOutcome.CorrectResultAndGoalDifference => ("CORRECT RESULT + GD", "#1565C0"),
            PredictionOutcome.CorrectResult => ("CORRECT RESULT", "#1565C0"),
            PredictionOutcome.CorrectGoalDifference => ("CORRECT GD", "#FF6B35"),
            _ => ("INCORRECT", "#C62828")
        };

        var hs = match.HomeScore ?? 0;
        var aws = match.AwayScore ?? 0;
        var ph = pred.PredictedHomeScore ?? 0;
        var pa = pred.PredictedAwayScore ?? 0;

        var svg = BuildSvg(pred.DisplayName, match.Home, match.Away, hs, aws, ph, pa, label, color, points);
        var ogHtml = BuildOgHtml(pred.DisplayName, match.Home, match.Away, hs, aws, ph, pa, label, points, query.MatchId, query.UserId);

        return ScoreCastResponse<PredictionReplayCardResult>.Ok(new PredictionReplayCardResult(svg, ogHtml));
    }

    private static string Esc(string s) => s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

    private static string BuildSvg(string name, string home, string away, int hs, int aws, int ph, int pa, string label, string color, int pts)
    {
        var bw = label.Length * 14 + 40;
        return $"""
            <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="630" viewBox="0 0 1200 630">
            <defs><linearGradient id="bg" x1="0" y1="0" x2="1" y2="1"><stop offset="0%" stop-color="#0A1929"/><stop offset="100%" stop-color="#37003C"/></linearGradient></defs>
            <rect width="1200" height="630" fill="url(#bg)"/>
            <text x="600" y="60" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="20" font-weight="700" opacity="0.5">SCORECAST</text>
            <text x="600" y="110" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="16" font-weight="600" opacity="0.4" letter-spacing="0.15em">FULL TIME</text>
            <text x="340" y="185" text-anchor="end" fill="white" font-family="Inter,system-ui,sans-serif" font-size="36" font-weight="700">{Esc(home)}</text>
            <text x="600" y="190" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="64" font-weight="800">{hs} – {aws}</text>
            <text x="860" y="185" text-anchor="start" fill="white" font-family="Inter,system-ui,sans-serif" font-size="36" font-weight="700">{Esc(away)}</text>
            <line x1="300" y1="230" x2="900" y2="230" stroke="white" stroke-opacity="0.15" stroke-width="1"/>
            <text x="600" y="280" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="22" font-weight="600" opacity="0.7">{Esc(name)}'s Prediction</text>
            <text x="600" y="350" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="72" font-weight="800">{ph} – {pa}</text>
            <rect x="{600 - bw / 2}" y="385" width="{bw}" height="40" rx="20" fill="{color}"/>
            <text x="600" y="412" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="16" font-weight="700">{label} · {pts} pts</text>
            <text x="600" y="580" text-anchor="middle" fill="white" font-family="Inter,system-ui,sans-serif" font-size="18" font-weight="600" opacity="0.3">scorecast.uk</text>
            </svg>
            """;
    }

    private static string BuildOgHtml(string name, string home, string away, int hs, int aws, int ph, int pa, string label, int pts, long matchId, long userId)
    {
        var title = Esc($"{name} predicted {home} {ph}-{pa} {away}");
        var desc = Esc($"Result: {home} {hs}-{aws} {away} — {label}");
        var img = $"https://scorecast.uk/api/v1/share/replay/{matchId}/{userId}";
        var page = $"https://scorecast.uk/replay/{matchId}/0";

        return $"""
            <!DOCTYPE html><html><head>
            <meta charset="utf-8"/><title>{title} | ScoreCast</title>
            <meta property="og:title" content="{title}"/>
            <meta property="og:description" content="{desc}"/>
            <meta property="og:image" content="{img}"/>
            <meta property="og:image:width" content="1200"/><meta property="og:image:height" content="630"/>
            <meta property="og:url" content="{page}"/><meta property="og:type" content="website"/><meta property="og:site_name" content="ScoreCast"/>
            <meta name="twitter:card" content="summary_large_image"/>
            <meta name="twitter:title" content="{title}"/><meta name="twitter:description" content="{desc}"/><meta name="twitter:image" content="{img}"/>
            <meta http-equiv="refresh" content="0;url={page}"/>
            </head><body><p>Redirecting to <a href="{page}">ScoreCast</a>...</p></body></html>
            """;
    }
}
