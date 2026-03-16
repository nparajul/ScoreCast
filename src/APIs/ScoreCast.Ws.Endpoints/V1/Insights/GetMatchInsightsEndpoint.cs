using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Endpoints.V1.Insights;

public sealed class GetMatchInsightsEndpoint
    : Endpoint<GetMatchInsightsRequest, ScoreCastResponse<List<MatchInsightResult>>>
{
    public override void Configure()
    {
        Get("upcoming");
        Group<InsightsGroup>();
        Summary(s => s.Description = "Get AI-powered insights for upcoming matches");
    }

    public override async Task HandleAsync(GetMatchInsightsRequest req, CancellationToken ct)
    {
        var db = Resolve<IScoreCastDbContext>();
        var chatClient = TryResolve<IChatClient>();

        var matches = await db.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == req.SeasonId
                        && m.Gameweek.Number == req.GameweekNumber
                        && m.Status == MatchStatus.Scheduled)
            .OrderBy(m => m.KickoffTime)
            .Select(m => new
            {
                m.Id, m.KickoffTime,
                HomeName = m.HomeTeam.Name, HomeLogo = m.HomeTeam.LogoUrl, HomeId = m.HomeTeamId,
                AwayName = m.AwayTeam.Name, AwayLogo = m.AwayTeam.LogoUrl, AwayId = m.AwayTeamId
            })
            .ToListAsync(ct);

        if (matches.Count == 0)
        {
            await Send.OkAsync(ScoreCastResponse<List<MatchInsightResult>>.Ok([]), ct);
            return;
        }

        var teamIds = matches.SelectMany(m => new[] { m.HomeId, m.AwayId }).Distinct().ToList();
        var recentResults = await db.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == req.SeasonId
                        && m.Status == MatchStatus.Finished
                        && (teamIds.Contains(m.HomeTeamId) || teamIds.Contains(m.AwayTeamId)))
            .OrderByDescending(m => m.KickoffTime)
            .Select(m => new { m.HomeTeamId, m.AwayTeamId, m.HomeScore, m.AwayScore })
            .ToListAsync(ct);

        var teamForm = teamIds.ToDictionary(id => id, id =>
        {
            var results = recentResults
                .Where(r => r.HomeTeamId == id || r.AwayTeamId == id)
                .Take(5)
                .Select(r =>
                {
                    var isHome = r.HomeTeamId == id;
                    var scored = isHome ? r.HomeScore ?? 0 : r.AwayScore ?? 0;
                    var conceded = isHome ? r.AwayScore ?? 0 : r.HomeScore ?? 0;
                    return scored > conceded ? 3 : scored == conceded ? 1 : 0;
                }).ToList();
            return results.Count > 0 ? (double)results.Sum() / (results.Count * 3) : 0.5;
        });

        var insights = matches.Select(m =>
        {
            var homeStr = teamForm.GetValueOrDefault(m.HomeId, 0.5);
            var awayStr = teamForm.GetValueOrDefault(m.AwayId, 0.5);
            homeStr = Math.Min(1.0, homeStr + 0.1);
            var total = homeStr + awayStr + 0.3;
            var homePct = (int)(homeStr / total * 100);
            var awayPct = (int)(awayStr / total * 100);
            var drawPct = 100 - homePct - awayPct;

            return new MatchInsightResult(m.Id, m.HomeName, m.HomeLogo, m.AwayName, m.AwayLogo,
                m.KickoffTime, homePct, drawPct, awayPct, null);
        }).ToList();

        if (chatClient is not null)
        {
            var prompt = "You are a football pundit. For each match, write ONE exciting 1-2 sentence hype line. Be bold. Return ONLY a JSON array of strings, same order.\n\nMatches:\n"
                + string.Join("\n", insights.Select((ins, i) =>
                    $"{i + 1}. {ins.HomeTeamName} vs {ins.AwayTeamName} (Home {ins.HomeWinPct}%, Draw {ins.DrawPct}%, Away {ins.AwayWinPct}%)"));

            try
            {
                var response = await chatClient.GetResponseAsync(prompt, cancellationToken: ct);
                var raw = response.Text?.Trim() ?? "";
                // Strip markdown code fences if present
                if (raw.StartsWith("```"))
                {
                    raw = raw.Split('\n', 2).Length > 1 ? raw.Split('\n', 2)[1] : raw;
                    raw = raw.TrimEnd('`').Trim();
                }
                var text = raw.TrimStart('[').TrimEnd(']');
                var summaries = text.Split("\",")
                    .Select(s => s.Trim().Trim('"').Trim())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();

                for (var i = 0; i < Math.Min(summaries.Count, insights.Count); i++)
                    insights[i] = insights[i] with { AiSummary = summaries[i] };
            }
            catch { /* AI is optional */ }
        }

        await Send.OkAsync(ScoreCastResponse<List<MatchInsightResult>>.Ok(insights), ct);
    }
}
