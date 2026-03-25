using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Football.Commands;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Domain.V1.Entities.Football;
using ScoreCast.Shared.Enums;

using ScoreCast.Shared.Types;

namespace ScoreCast.Ws.Infrastructure.V1.Football.CommandHandlers;

internal sealed partial record GetMatchHighlightsCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory) : ICommandHandler<GetMatchHighlightsCommand, ScoreCastResponse<MatchHighlightsResult>>
{
    private static readonly MatchHighlightsResult Empty = new([]);
    private static readonly Dictionary<long, DateTime> _missCache = new();

    public async Task<ScoreCastResponse<MatchHighlightsResult>> ExecuteAsync(
        GetMatchHighlightsCommand query, CancellationToken ct)
    {
        var match = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Id == query.MatchId)
            .Select(m => new { HomeName = m.HomeTeam.Name, AwayName = m.AwayTeam.Name, HomeShort = m.HomeTeam.ShortName, AwayShort = m.AwayTeam.ShortName, m.Status })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return ScoreCastResponse<MatchHighlightsResult>.Ok(Empty);

        // Live matches: fetch from Scorebat
        if (match.Status == MatchStatus.Live)
            return await FetchScorebatLiveAsync(match.HomeName, match.HomeShort, match.AwayName, match.AwayShort, ct);

        if (match.Status != MatchStatus.Finished)
            return ScoreCastResponse<MatchHighlightsResult>.Ok(Empty);

        var cached = await DbContext.MatchHighlights
            .AsNoTracking()
            .Where(h => h.MatchId == query.MatchId && h.Type == HighlightType.Highlight)
            .Select(h => new HighlightVideo(h.Title, h.EmbedHtml))
            .ToListAsync(ct);

        if (cached.Count > 0)
            return ScoreCastResponse<MatchHighlightsResult>.Ok(new MatchHighlightsResult(cached));

        // Skip if we already searched recently and found nothing
        lock (_missCache)
        {
            if (_missCache.TryGetValue(query.MatchId, out var lastMiss) &&
                (ScoreCastDateTime.Now.Value - lastMiss).TotalMinutes < 60)
                return ScoreCastResponse<MatchHighlightsResult>.Ok(Empty);
        }

        // Scrape YouTube search
        try
        {
            var home = Normalize(match.HomeName);
            var away = Normalize(match.AwayName);
            var http = HttpClientFactory.CreateClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
            var html = await http.GetStringAsync(
                $"https://www.youtube.com/results?search_query={Uri.EscapeDataString($"{home} vs {away} highlights Premier League")}", ct);

            string? vid = null;
            foreach (var m in VideoIdRegex().Matches(html).Cast<System.Text.RegularExpressions.Match>())
            {
                var candidate = m.Groups[1].Value;
                var resp = await http.GetAsync(
                    $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={candidate}&format=json", ct);
                if (resp.IsSuccessStatusCode) { vid = candidate; break; }
            }

            if (vid is null)
            {
                lock (_missCache) { _missCache[query.MatchId] = ScoreCastDateTime.Now.Value; }
                return ScoreCastResponse<MatchHighlightsResult>.Ok(Empty);
            }

            var title = "Highlights";
            var embedHtml = $"<iframe src='https://www.youtube-nocookie.com/embed/{vid}?autoplay=1&amp;mute=1&amp;modestbranding=1&amp;rel=0&amp;iv_load_policy=3&amp;playsinline=1&amp;controls=1' frameborder='0' allowfullscreen allow='autoplay; fullscreen; encrypted-media' style='width:100%;height:100%;'></iframe>";

            DbContext.MatchHighlights.Add(new MatchHighlight { MatchId = query.MatchId, Title = title, EmbedHtml = embedHtml, Type = ScoreCast.Shared.Enums.HighlightType.Highlight });
            await UnitOfWork.SaveChangesAsync(nameof(GetMatchHighlightsCommand), ct);

            return ScoreCastResponse<MatchHighlightsResult>.Ok(
                new MatchHighlightsResult([new HighlightVideo(title, embedHtml)]));
        }
        catch
        {
            return ScoreCastResponse<MatchHighlightsResult>.Ok(Empty);
        }
    }

    private static string Normalize(string name) =>
        name.Replace(" FC", "").Replace(" AFC", "").Trim();

    private static string NormalizeLower(string name) =>
        name.ToLowerInvariant().Replace(" fc", "").Replace(" afc", "").Trim();

    private async Task<ScoreCastResponse<MatchHighlightsResult>> FetchScorebatLiveAsync(
        string homeName, string? homeShort, string awayName, string? awayShort, CancellationToken ct)
    {
        try
        {
            var http = HttpClientFactory.CreateClient();
            var feed = await http.GetFromJsonAsync<List<SbMatch>>(
                "https://www.scorebat.com/video-api/v1/", ct) ?? [];

            var found = feed.FirstOrDefault(m =>
                SbNameMatch(m.Side1?.Name, homeName, homeShort) &&
                SbNameMatch(m.Side2?.Name, awayName, awayShort));

            if (found?.Embed is null)
                return ScoreCastResponse<MatchHighlightsResult>.Ok(Empty);

            return ScoreCastResponse<MatchHighlightsResult>.Ok(
                new MatchHighlightsResult([new HighlightVideo("🔴 LIVE Goals", found.Embed)]));
        }
        catch { return ScoreCastResponse<MatchHighlightsResult>.Ok(Empty); }
    }

    private static bool SbNameMatch(string? sbName, string dbName, string? shortName)
    {
        if (string.IsNullOrEmpty(sbName)) return false;
        var sb = NormalizeLower(sbName);
        var db = NormalizeLower(dbName);
        if (sb.Contains(db) || db.Contains(sb)) return true;
        if (shortName is not null)
        {
            var sn = NormalizeLower(shortName);
            if (sb.Contains(sn) || sn.Contains(sb)) return true;
        }
        return false;
    }

    [GeneratedRegex("\"videoId\":\"([^\"]+)\"")]
    private static partial Regex VideoIdRegex();
}

file class SbMatch
{
    [JsonPropertyName("embed")] public string? Embed { get; set; }
    [JsonPropertyName("side1")] public SbSide? Side1 { get; set; }
    [JsonPropertyName("side2")] public SbSide? Side2 { get; set; }
}

file class SbSide
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}
