using System.Text.Json;
using System.Text.RegularExpressions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ScoreCast.Models.V1.Requests.Insights;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Insights.Commands;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Infrastructure.V1.Insights.CommandHandlers;

internal sealed record GetOrCreateMatchInsightsCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory,
    IConfiguration Configuration,
    IChatClient? ChatClient = null)
    : ICommandHandler<GetOrCreateMatchInsightsCommand, ScoreCastResponse<List<MatchInsightResult>>>
{
    public async Task<ScoreCastResponse<List<MatchInsightResult>>> ExecuteAsync(
        GetOrCreateMatchInsightsCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var cached = await DbContext.MatchInsightCaches
            .FirstOrDefaultAsync(c => c.SeasonId == request.SeasonId && c.GameweekNumber == request.GameweekNumber, ct);

        if (cached is not null)
        {
            var results = JsonSerializer.Deserialize<List<MatchInsightResult>>(cached.ResponseJson) ?? [];
            if (results.Count > 0 && results.All(r => r.HomeTeamId > 0))
                return ScoreCastResponse<List<MatchInsightResult>>.Ok(results);
            // Stale cache missing team IDs — regenerate
            DbContext.MatchInsightCaches.Remove(cached);
            await UnitOfWork.SaveChangesAsync(nameof(GetOrCreateMatchInsightsCommand), ct);
        }

        var season = await DbContext.Seasons
            .AsNoTracking()
            .Include(s => s.Competition)
            .FirstOrDefaultAsync(s => s.Id == request.SeasonId, ct);

        if (season is null)
            return ScoreCastResponse<List<MatchInsightResult>>.Ok([]);

        var matches = await GetScheduledMatches(request, ct);
        if (matches.Count == 0)
            return ScoreCastResponse<List<MatchInsightResult>>.Ok([]);

        var teamIds = matches.SelectMany(m => new[] { m.HomeId, m.AwayId }).Distinct().ToList();
        var recentResults = await GetRecentResults(request.SeasonId, teamIds, ct);
        var h2hResults = await GetH2HResults(teamIds, ct);
        var teamStats = BuildTeamStats(teamIds, recentResults);

        // Scrape real standings + news from BBC
        var bbcSlug = GetBbcSlug(season.Competition.Code);
        var bbcBaseUrl = Configuration["Scraping:BbcBaseUrl"] ?? "https://www.bbc.co.uk/sport/football";
        var http = HttpClientFactory.CreateClient();
        var standingsTask = ScrapeStandings(http, bbcBaseUrl, bbcSlug, ct);
        var headlinesTask = ScrapeHeadlines(http, bbcBaseUrl, bbcSlug, ct);
        await Task.WhenAll(standingsTask, headlinesTask);
        var standings = standingsTask.Result;
        var headlines = headlinesTask.Result;

        var insights = matches.Select(m => BuildInsight(m, teamStats, standings)).ToList();

        // Enrich with Poisson model predictions
        await EnrichWithPoisson(matches, insights, ct);

        if (ChatClient is not null)
            await EnrichWithAi(matches, insights, teamStats, standings, h2hResults, headlines,
                season.Competition.Name, ct);

        DbContext.MatchInsightCaches.Add(new MatchInsightCache
        {
            SeasonId = request.SeasonId,
            GameweekNumber = request.GameweekNumber,
            ResponseJson = JsonSerializer.Serialize(insights),
            CreatedByApp = request.AppName ?? nameof(GetOrCreateMatchInsightsCommand)
        });
        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(GetOrCreateMatchInsightsCommand), ct);

        return ScoreCastResponse<List<MatchInsightResult>>.Ok(insights);
    }

    private async Task<List<MatchData>> GetScheduledMatches(GetMatchInsightsRequest request, CancellationToken ct) =>
        await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == request.SeasonId
                        && m.Gameweek.Number == request.GameweekNumber
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

    private async Task<List<ResultData>> GetH2HResults(List<long> teamIds, CancellationToken ct) =>
        await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Status == MatchStatus.Finished
                        && teamIds.Contains(m.HomeTeamId) && teamIds.Contains(m.AwayTeamId))
            .OrderByDescending(m => m.KickoffTime)
            .Take(100)
            .Select(m => new ResultData(m.HomeTeamId, m.AwayTeamId, m.HomeScore, m.AwayScore))
            .ToListAsync(ct);

    private static Dictionary<long, TeamStat> BuildTeamStats(List<long> teamIds, List<ResultData> results) =>
        teamIds.ToDictionary(id => id, id =>
        {
            var all = results.Where(r => r.HomeTeamId == id || r.AwayTeamId == id).ToList();
            var last5 = all.Take(5).Select(r => Outcome(r, id)).ToList();
            var form = last5.Count > 0 ? string.Join("", last5) : "?";
            var formPct = last5.Count > 0
                ? (last5.Count(f => f == "W") * 3.0 + last5.Count(f => f == "D")) / (last5.Count * 3)
                : 0.5;
            return new TeamStat(form, formPct);
        });

    private static string Outcome(ResultData r, long teamId)
    {
        var scored = r.HomeTeamId == teamId ? r.HomeScore ?? 0 : r.AwayScore ?? 0;
        var conceded = r.HomeTeamId == teamId ? r.AwayScore ?? 0 : r.HomeScore ?? 0;
        return scored > conceded ? "W" : scored == conceded ? "D" : "L";
    }

    private static async Task EnrichWithPoisson(List<MatchData> matches, List<MatchInsightResult> insights, CancellationToken ct)
    {
        for (var i = 0; i < matches.Count && i < insights.Count; i++)
        {
            try
            {
                var result = await new GetMatchPredictionQuery(matches[i].Id).ExecuteAsync(ct);
                if (result is not { Success: true, Data: { } p }) continue;
                var top = p.TopScorelines.FirstOrDefault();
                insights[i] = insights[i] with
                {
                    HomeWinPct = p.HomeWinPct,
                    DrawPct = p.DrawPct,
                    AwayWinPct = p.AwayWinPct,
                    HomeXg = p.HomeExpectedGoals,
                    AwayXg = p.AwayExpectedGoals,
                    TopScoreline = top is not null ? $"{top.Home}-{top.Away}" : null,
                    TopScorelinePct = top?.Pct
                };
            }
            catch { /* prediction failed for this match, keep form-based values */ }
        }
    }

    private static MatchInsightResult BuildInsight(
        MatchData m, Dictionary<long, TeamStat> stats, Dictionary<string, StandingRow> standings)
    {
        var homeStr = stats.GetValueOrDefault(m.HomeId)?.FormPct ?? 0.5;
        var awayStr = stats.GetValueOrDefault(m.AwayId)?.FormPct ?? 0.5;
        homeStr = Math.Min(1.0, homeStr + 0.1); // home advantage
        var total = homeStr + awayStr + 0.3;
        var homePct = (int)(homeStr / total * 100);
        var awayPct = (int)(awayStr / total * 100);
        var drawPct = 100 - homePct - awayPct;

        return new MatchInsightResult(m.Id, m.HomeId, m.HomeName, m.HomeShort, m.HomeLogo,
            m.AwayId, m.AwayName, m.AwayShort, m.AwayLogo, m.KickoffTime, homePct, drawPct, awayPct, null);
    }

    // --- BBC scraping ---

    private static string GetBbcSlug(string code) => code switch
    {
        "PL" => "premier-league",
        "ELC" => "championship",
        "BL1" => "german-bundesliga",
        "SA" => "italian-serie-a",
        "PD" => "spanish-la-liga",
        "FL1" => "french-ligue-one",
        _ => "premier-league"
    };

    private static async Task<Dictionary<string, StandingRow>> ScrapeStandings(
        HttpClient http, string baseUrl, string slug, CancellationToken ct)
    {
        try
        {
            var html = await http.GetStringAsync($"{baseUrl}/{slug}/table", ct);
            var result = new Dictionary<string, StandingRow>(StringComparer.OrdinalIgnoreCase);

            // Pattern: position, team name, played, won, drawn, lost, GF, GA, GD, points
            var rows = Regex.Matches(html,
                @"<td[^>]*class=""[^""]*gs-o-table__cell[^""]*""[^>]*>(\d+)</td>\s*" +
                @"<td[^>]*>.*?<(?:span|abbr)[^>]*>([^<]+)</(?:span|abbr)>.*?</td>\s*" +
                @"<td[^>]*>(\d+)</td>\s*<td[^>]*>(\d+)</td>\s*<td[^>]*>(\d+)</td>\s*<td[^>]*>(\d+)</td>\s*" +
                @"<td[^>]*>(\d+)</td>\s*<td[^>]*>(\d+)</td>\s*<td[^>]*>(-?\d+)</td>\s*<td[^>]*>(\d+)</td>",
                RegexOptions.Singleline);

            if (rows.Count == 0)
            {
                // Simpler fallback: parse the text table format BBC returns
                var lines = html.Split('\n');
                foreach (var line in lines)
                {
                    // Look for lines with team data patterns
                    var m = Regex.Match(line, @"^\s*(\d+)\s+.*?(\w[\w\s]+\w)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(\d+)\s+(-?\d+)\s+(\d+)");
                    if (m.Success)
                    {
                        var team = m.Groups[2].Value.Trim();
                        result[team] = new StandingRow(
                            int.Parse(m.Groups[1].Value), int.Parse(m.Groups[3].Value),
                            int.Parse(m.Groups[4].Value), int.Parse(m.Groups[5].Value),
                            int.Parse(m.Groups[6].Value), int.Parse(m.Groups[9].Value),
                            int.Parse(m.Groups[10].Value));
                    }
                }
            }
            else
            {
                foreach (System.Text.RegularExpressions.Match row in rows)
                {
                    var team = row.Groups[2].Value.Trim();
                    result[team] = new StandingRow(
                        int.Parse(row.Groups[1].Value), int.Parse(row.Groups[3].Value),
                        int.Parse(row.Groups[4].Value), int.Parse(row.Groups[5].Value),
                        int.Parse(row.Groups[6].Value), int.Parse(row.Groups[9].Value),
                        int.Parse(row.Groups[10].Value));
                }
            }

            return result;
        }
        catch { return []; }
    }

    private static async Task<List<string>> ScrapeHeadlines(
        HttpClient http, string baseUrl, string slug, CancellationToken ct)
    {
        try
        {
            var html = await http.GetStringAsync($"{baseUrl}/{slug}", ct);
            var headlines = Regex.Matches(html, @"<h3[^>]*>(.*?)</h3>", RegexOptions.Singleline)
                .Select(m => Regex.Replace(m.Groups[1].Value, "<[^>]+>", "").Trim())
                .Where(h => h.Length > 10 && h.Length < 200)
                .Distinct()
                .Take(15)
                .ToList();
            return headlines;
        }
        catch { return []; }
    }

    // --- AI enrichment ---

    private async Task EnrichWithAi(
        List<MatchData> matches, List<MatchInsightResult> insights,
        Dictionary<long, TeamStat> stats, Dictionary<string, StandingRow> standings,
        List<ResultData> h2hResults, List<string> headlines,
        string competitionName, CancellationToken ct)
    {
        var matchContext = insights.Select((ins, i) =>
        {
            var m = matches[i];
            var hs = stats.GetValueOrDefault(m.HomeId);
            var aStats = stats.GetValueOrDefault(m.AwayId);
            var ko = ins.KickoffTime?.ToString("ddd d MMM yyyy, HH:mm") ?? "TBD";

            var hStand = FindStanding(standings, m.HomeName, m.HomeShort);
            var aStand = FindStanding(standings, m.AwayName, m.AwayShort);
            var hPos = hStand is not null ? $"#{hStand.Position} ({hStand.Points}pts, W{hStand.Won} D{hStand.Drawn} L{hStand.Lost}, GD {hStand.GoalDiff:+0;-0})" : "N/A";
            var aPos = aStand is not null ? $"#{aStand.Position} ({aStand.Points}pts, W{aStand.Won} D{aStand.Drawn} L{aStand.Lost}, GD {aStand.GoalDiff:+0;-0})" : "N/A";

            var h2h = h2hResults
                .Where(r => (r.HomeTeamId == m.HomeId && r.AwayTeamId == m.AwayId)
                          || (r.HomeTeamId == m.AwayId && r.AwayTeamId == m.HomeId))
                .Take(5)
                .Select(r =>
                {
                    var homeTeam = r.HomeTeamId == m.HomeId ? m.HomeName : m.AwayName;
                    return $"{homeTeam} {r.HomeScore}-{r.AwayScore}";
                })
                .ToList();
            var h2hStr = h2h.Count > 0 ? string.Join(", ", h2h) : "no recent meetings";

            var line = $"{i + 1}. {ins.HomeTeamName} (HOME) [{hPos}, form:{hs?.Form}] vs {ins.AwayTeamName} (AWAY) [{aPos}, form:{aStats?.Form}] — {ko}";
            line += $"\n   H2H (last {h2h.Count}): {h2hStr}";
            line += $"\n   Win%: Home {ins.HomeWinPct}%, Draw {ins.DrawPct}%, Away {ins.AwayWinPct}%";
            return line;
        });

        var newsSection = headlines.Count > 0
            ? $"\n\nLatest {competitionName} headlines (use these for context on injuries, suspensions, managerial changes, transfer news):\n{string.Join("\n", headlines.Select(h => $"- {h}"))}"
            : "";

        var prompt = $"""
You are a sharp, opinionated football pundit covering {competitionName}. For each match below, write a bold 2-3 sentence preview that a fan would actually want to read.

RULES:
- Be SPECIFIC: reference actual form runs ("3 wins in a row"), league positions, goal differences, H2H patterns
- Be OPINIONATED: pick a likely winner or explain why it's a genuine toss-up. Don't sit on the fence
- Reference NEWS if relevant: injuries, suspensions, managerial changes, transfer drama from the headlines
- Mention STAKES: title race, top 4, relegation, European spots — whatever applies to these teams
- NEVER use generic filler like "this promises to be an exciting clash" or "both teams will be looking to"
- Each preview should feel different — vary your sentence structure and angle
- Keep it punchy: max 3 sentences, no fluff

Return ONLY a JSON array of strings, one per match, same order. No markdown, no code fences.

Matches:
{string.Join("\n", matchContext)}{newsSection}
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

            var summaries = JsonSerializer.Deserialize<List<string>>(raw);
            if (summaries is not null)
                for (var i = 0; i < Math.Min(summaries.Count, insights.Count); i++)
                    insights[i] = insights[i] with { AiSummary = summaries[i] };
        }
        catch { /* AI is optional */ }
    }

    private static StandingRow? FindStanding(Dictionary<string, StandingRow> standings, string name, string? shortName)
    {
        if (standings.Count == 0) return null;
        if (standings.TryGetValue(name, out var s)) return s;
        if (shortName is not null && standings.TryGetValue(shortName, out s)) return s;

        // Fuzzy: try matching on key words (e.g. "Arsenal FC" → "Arsenal")
        var key = standings.Keys.FirstOrDefault(k =>
            k.Contains(name.Split(' ')[0], StringComparison.OrdinalIgnoreCase) ||
            name.Contains(k.Split(' ')[0], StringComparison.OrdinalIgnoreCase));
        return key is not null ? standings[key] : null;
    }

    private sealed record MatchData(long Id, DateTime? KickoffTime,
        string HomeName, string? HomeShort, string? HomeLogo, long HomeId,
        string AwayName, string? AwayShort, string? AwayLogo, long AwayId);

    private sealed record ResultData(long HomeTeamId, long AwayTeamId, int? HomeScore, int? AwayScore);

    private sealed record TeamStat(string Form, double FormPct);

    private sealed record StandingRow(int Position, int Played, int Won, int Drawn, int Lost, int GoalDiff, int Points);
}
