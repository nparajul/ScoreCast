using System.Xml.Linq;
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
                HomeName = m.HomeTeam.Name, HomeShort = m.HomeTeam.ShortName, HomeLogo = m.HomeTeam.LogoUrl, HomeId = m.HomeTeamId,
                AwayName = m.AwayTeam.Name, AwayShort = m.AwayTeam.ShortName, AwayLogo = m.AwayTeam.LogoUrl, AwayId = m.AwayTeamId
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

        // Build form string (W/D/L) and points for each team
        var teamStats = teamIds.ToDictionary(id => id, id =>
        {
            var all = recentResults.Where(r => r.HomeTeamId == id || r.AwayTeamId == id).ToList();
            var last5 = all.Take(5).Select(r =>
            {
                var isHome = r.HomeTeamId == id;
                var scored = isHome ? r.HomeScore ?? 0 : r.AwayScore ?? 0;
                var conceded = isHome ? r.AwayScore ?? 0 : r.HomeScore ?? 0;
                return scored > conceded ? "W" : scored == conceded ? "D" : "L";
            }).ToList();
            var pts = all.Sum(r =>
            {
                var isHome = r.HomeTeamId == id;
                var scored = isHome ? r.HomeScore ?? 0 : r.AwayScore ?? 0;
                var conceded = isHome ? r.AwayScore ?? 0 : r.HomeScore ?? 0;
                return scored > conceded ? 3 : scored == conceded ? 1 : 0;
            });
            var formStr = last5.Count > 0 ? string.Join("", last5) : "?";
            var formPct = last5.Count > 0 ? (double)last5.Count(f => f == "W") * 3 / (last5.Count * 3)
                + (double)last5.Count(f => f == "D") / (last5.Count * 3) : 0.5;
            return (Form: formStr, Points: pts, Played: all.Count, FormPct: formPct);
        });

        // Rank teams by points for league position
        var ranked = teamStats.OrderByDescending(t => t.Value.Points).Select((t, i) => (t.Key, Pos: i + 1)).ToDictionary(x => x.Key, x => x.Pos);

        var insights = matches.Select(m =>
        {
            var homeStr = teamStats.GetValueOrDefault(m.HomeId).FormPct;
            var awayStr = teamStats.GetValueOrDefault(m.AwayId).FormPct;
            if (homeStr == 0) homeStr = 0.5;
            if (awayStr == 0) awayStr = 0.5;
            homeStr = Math.Min(1.0, homeStr + 0.1);
            var total = homeStr + awayStr + 0.3;
            var homePct = (int)(homeStr / total * 100);
            var awayPct = (int)(awayStr / total * 100);
            var drawPct = 100 - homePct - awayPct;

            return new MatchInsightResult(m.Id, m.HomeName, m.HomeShort, m.HomeLogo, m.AwayName, m.AwayShort, m.AwayLogo,
                m.KickoffTime, homePct, drawPct, awayPct, null);
        }).ToList();

        if (chatClient is not null)
        {
            var http = Resolve<IHttpClientFactory>().CreateClient();
            var teamNames = matches.SelectMany(m => new[] { m.HomeName, m.AwayName }).Distinct().ToList();
            var newsMap = new Dictionary<string, string>();

            // Fetch latest headlines per team from Google News RSS (parallel, best-effort)
            var newsTasks = teamNames.Select(async name =>
            {
                try
                {
                    var q = Uri.EscapeDataString($"{name} Premier League");
                    var rss = await http.GetStringAsync($"https://news.google.com/rss/search?q={q}&hl=en-US&gl=US&ceid=US:en", ct);
                    var doc = XDocument.Parse(rss);
                    var headlines = doc.Descendants("item")
                        .Take(3)
                        .Select(item => item.Element("title")?.Value)
                        .Where(t => t is not null);
                    return (name, News: string.Join(" | ", headlines));
                }
                catch { return (name, News: ""); }
            });
            foreach (var result in await Task.WhenAll(newsTasks))
                if (!string.IsNullOrWhiteSpace(result.News))
                    newsMap[result.name] = result.News;

            var totalTeams = teamStats.Count > 0 ? ranked.Values.Max() : 20;
            var matchContext = insights.Select((ins, i) =>
            {
                var hId = matches[i].HomeId;
                var aId = matches[i].AwayId;
                var hs = teamStats.GetValueOrDefault(hId);
                var aStats = teamStats.GetValueOrDefault(aId);
                var hPos = ranked.GetValueOrDefault(hId);
                var aPos = ranked.GetValueOrDefault(aId);
                var ko = ins.KickoffTime?.ToString("ddd d MMM yyyy, HH:mm") ?? "TBD";
                var line = $"{i + 1}. {ins.HomeTeamName} (#{hPos}, {hs.Points}pts, form:{hs.Form}) vs {ins.AwayTeamName} (#{aPos}, {aStats.Points}pts, form:{aStats.Form}) — {ko} — Home win {ins.HomeWinPct}%, Draw {ins.DrawPct}%, Away {ins.AwayWinPct}%";
                var homeNews = newsMap.GetValueOrDefault(matches[i].HomeName, "");
                var awayNews = newsMap.GetValueOrDefault(matches[i].AwayName, "");
                if (!string.IsNullOrWhiteSpace(homeNews))
                    line += $"\n   {ins.HomeTeamName} news: {homeNews}";
                if (!string.IsNullOrWhiteSpace(awayNews))
                    line += $"\n   {ins.AwayTeamName} news: {awayNews}";
                return line;
            });

            var prompt = $"""
You are an expert Premier League analyst. For each match write ONE punchy 2-sentence preview. Reference league position, recent form, what's at stake, and incorporate the latest news headlines provided. Mention specific injuries, transfers, managerial changes, or storylines from the news. Be specific and insightful. {totalTeams} teams in the league.
Return ONLY a JSON array of strings, one per match, same order.

Matches:
{string.Join("\n", matchContext)}
""";

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
