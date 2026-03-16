using System.Text.Json;
using System.Xml.Linq;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Insights.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Infrastructure.V1.Insights.QueryHandlers;

internal sealed record GetMatchInsightsQueryHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory,
    IChatClient? ChatClient = null)
    : IQueryHandler<GetMatchInsightsQuery, ScoreCastResponse<List<MatchInsightResult>>>
{
    public async Task<ScoreCastResponse<List<MatchInsightResult>>> ExecuteAsync(
        GetMatchInsightsQuery query, CancellationToken ct)
    {
        // Check cache first
        var cached = await DbContext.MatchInsightCaches
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.SeasonId == query.SeasonId && c.GameweekNumber == query.GameweekNumber, ct);

        if (cached is not null)
            return ScoreCastResponse<List<MatchInsightResult>>.Ok(
                JsonSerializer.Deserialize<List<MatchInsightResult>>(cached.ResponseJson) ?? []);

        var matches = await GetScheduledMatches(query, ct);
        if (matches.Count == 0)
            return ScoreCastResponse<List<MatchInsightResult>>.Ok([]);

        var teamIds = matches.SelectMany(m => new[] { m.HomeId, m.AwayId }).Distinct().ToList();
        var recentResults = await GetRecentResults(query.SeasonId, teamIds, ct);
        var teamStats = BuildTeamStats(teamIds, recentResults);
        var ranked = RankTeams(teamStats);
        var insights = matches.Select(m => BuildInsight(m, teamStats, ranked)).ToList();

        if (ChatClient is not null)
            await EnrichWithAi(matches, insights, teamStats, ranked, ct);

        // Save to cache
        DbContext.MatchInsightCaches.Add(new MatchInsightCache
        {
            SeasonId = query.SeasonId,
            GameweekNumber = query.GameweekNumber,
            ResponseJson = JsonSerializer.Serialize(insights),
            CreatedByApp = "ScoreCast"
        });
        await UnitOfWork.SaveChangesAsync(nameof(GetMatchInsightsQuery), ct);

        return ScoreCastResponse<List<MatchInsightResult>>.Ok(insights);
    }

    private async Task<List<MatchData>> GetScheduledMatches(GetMatchInsightsQuery query, CancellationToken ct) =>
        await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == query.SeasonId
                        && m.Gameweek.Number == query.GameweekNumber
                        && m.Status == MatchStatus.Scheduled)
            .OrderBy(m => m.KickoffTime)
            .Select(m => new MatchData(m.Id, m.KickoffTime,
                m.HomeTeam.Name, m.HomeTeam.ShortName, m.HomeTeam.LogoUrl, m.HomeTeamId,
                m.AwayTeam.Name, m.AwayTeam.ShortName, m.AwayTeam.LogoUrl, m.AwayTeamId))
            .ToListAsync(ct);

    private async Task<List<ResultData>> GetRecentResults(long seasonId, List<long> teamIds, CancellationToken ct) =>
        await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == seasonId
                        && m.Status == MatchStatus.Finished
                        && (teamIds.Contains(m.HomeTeamId) || teamIds.Contains(m.AwayTeamId)))
            .OrderByDescending(m => m.KickoffTime)
            .Select(m => new ResultData(m.HomeTeamId, m.AwayTeamId, m.HomeScore, m.AwayScore))
            .ToListAsync(ct);

    private static Dictionary<long, TeamStat> BuildTeamStats(List<long> teamIds, List<ResultData> results) =>
        teamIds.ToDictionary(id => id, id =>
        {
            var all = results.Where(r => r.HomeTeamId == id || r.AwayTeamId == id).ToList();
            var last5 = all.Take(5).Select(r => Outcome(r, id)).ToList();
            var pts = all.Sum(r => Outcome(r, id) switch { "W" => 3, "D" => 1, _ => 0 });
            var form = last5.Count > 0 ? string.Join("", last5) : "?";
            var formPct = last5.Count > 0
                ? (last5.Count(f => f == "W") * 3.0 + last5.Count(f => f == "D")) / (last5.Count * 3)
                : 0.5;
            return new TeamStat(form, pts, formPct);
        });

    private static string Outcome(ResultData r, long teamId)
    {
        var scored = r.HomeTeamId == teamId ? r.HomeScore ?? 0 : r.AwayScore ?? 0;
        var conceded = r.HomeTeamId == teamId ? r.AwayScore ?? 0 : r.HomeScore ?? 0;
        return scored > conceded ? "W" : scored == conceded ? "D" : "L";
    }

    private static Dictionary<long, int> RankTeams(Dictionary<long, TeamStat> stats) =>
        stats.OrderByDescending(t => t.Value.Points)
            .Select((t, i) => (t.Key, Pos: i + 1))
            .ToDictionary(x => x.Key, x => x.Pos);

    private static MatchInsightResult BuildInsight(
        MatchData m, Dictionary<long, TeamStat> stats, Dictionary<long, int> ranked)
    {
        var homeStr = stats.GetValueOrDefault(m.HomeId)?.FormPct ?? 0.5;
        var awayStr = stats.GetValueOrDefault(m.AwayId)?.FormPct ?? 0.5;
        homeStr = Math.Min(1.0, homeStr + 0.1);
        var total = homeStr + awayStr + 0.3;
        var homePct = (int)(homeStr / total * 100);
        var awayPct = (int)(awayStr / total * 100);
        var drawPct = 100 - homePct - awayPct;

        return new MatchInsightResult(m.Id, m.HomeName, m.HomeShort, m.HomeLogo,
            m.AwayName, m.AwayShort, m.AwayLogo, m.KickoffTime, homePct, drawPct, awayPct, null);
    }

    private async Task EnrichWithAi(
        List<MatchData> matches, List<MatchInsightResult> insights,
        Dictionary<long, TeamStat> stats, Dictionary<long, int> ranked, CancellationToken ct)
    {
        var newsMap = await FetchTeamNews(matches, ct);
        var totalTeams = ranked.Count > 0 ? ranked.Values.Max() : 20;

        var matchContext = insights.Select((ins, i) =>
        {
            var hs = stats.GetValueOrDefault(matches[i].HomeId);
            var aStats = stats.GetValueOrDefault(matches[i].AwayId);
            var hPos = ranked.GetValueOrDefault(matches[i].HomeId);
            var aPos = ranked.GetValueOrDefault(matches[i].AwayId);
            var ko = ins.KickoffTime?.ToString("ddd d MMM yyyy, HH:mm") ?? "TBD";
            var line = $"{i + 1}. {ins.HomeTeamName} (#{hPos}, {hs?.Points}pts, form:{hs?.Form}) vs {ins.AwayTeamName} (#{aPos}, {aStats?.Points}pts, form:{aStats?.Form}) — {ko} — Home win {ins.HomeWinPct}%, Draw {ins.DrawPct}%, Away {ins.AwayWinPct}%";
            if (newsMap.TryGetValue(matches[i].HomeName, out var homeNews))
                line += $"\n   {ins.HomeTeamName} news: {homeNews}";
            if (newsMap.TryGetValue(matches[i].AwayName, out var awayNews))
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
            var response = await ChatClient!.GetResponseAsync(prompt, cancellationToken: ct);
            var raw = response.Text?.Trim() ?? "";
            if (raw.StartsWith("```"))
            {
                raw = raw.Split('\n', 2).Length > 1 ? raw.Split('\n', 2)[1] : raw;
                raw = raw.TrimEnd('`').Trim();
            }
            var summaries = raw.TrimStart('[').TrimEnd(']')
                .Split("\",")
                .Select(s => s.Trim().Trim('"').Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();

            for (var i = 0; i < Math.Min(summaries.Count, insights.Count); i++)
                insights[i] = insights[i] with { AiSummary = summaries[i] };
        }
        catch { /* AI is optional */ }
    }

    private async Task<Dictionary<string, string>> FetchTeamNews(List<MatchData> matches, CancellationToken ct)
    {
        var http = HttpClientFactory.CreateClient();
        var teamNames = matches.SelectMany(m => new[] { m.HomeName, m.AwayName }).Distinct();
        var tasks = teamNames.Select(async name =>
        {
            try
            {
                var q = Uri.EscapeDataString($"{name} Premier League");
                var rss = await http.GetStringAsync(
                    $"https://news.google.com/rss/search?q={q}&hl=en-US&gl=US&ceid=US:en", ct);
                var headlines = XDocument.Parse(rss).Descendants("item")
                    .Take(3)
                    .Select(item => item.Element("title")?.Value)
                    .Where(t => t is not null);
                return (name, News: string.Join(" | ", headlines));
            }
            catch { return (name, News: ""); }
        });

        return (await Task.WhenAll(tasks))
            .Where(r => !string.IsNullOrWhiteSpace(r.News))
            .ToDictionary(r => r.name, r => r.News);
    }

    private sealed record MatchData(long Id, DateTime? KickoffTime,
        string HomeName, string? HomeShort, string? HomeLogo, long HomeId,
        string AwayName, string? AwayShort, string? AwayLogo, long AwayId);

    private sealed record ResultData(long HomeTeamId, long AwayTeamId, int? HomeScore, int? AwayScore);

    private sealed record TeamStat(string Form, int Points, double FormPct);
}
