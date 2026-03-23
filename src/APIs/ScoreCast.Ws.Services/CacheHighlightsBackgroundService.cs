using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Services;

public sealed partial class CacheHighlightsBackgroundService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<CacheHighlightsBackgroundService> logger) : BackgroundService
{
    private DateTime _lastYouTubeRun = DateTime.MinValue;

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), ct);

        // One-time cleanup of private/unavailable videos
        await PurgeUnavailableAsync(ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                if ((ScoreCastDateTime.Now.Value - _lastYouTubeRun).TotalHours >= 1)
                {
                    await CacheYouTubeAsync(ct);
                    _lastYouTubeRun = ScoreCastDateTime.Now.Value;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "CacheHighlights background failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(2), ct);
        }
    }

    private async Task CacheYouTubeAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IScoreCastDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var matches = await db.Matches.AsNoTracking()
            .Where(m => m.Status == MatchStatus.Finished && m.KickoffTime != null)
            .OrderByDescending(m => m.KickoffTime)
            .Take(50)
            .Select(m => new { m.Id, HomeName = m.HomeTeam.Name, AwayName = m.AwayTeam.Name })
            .ToListAsync(ct);

        var existing = await db.MatchHighlights.AsNoTracking()
            .Where(h => h.EmbedHtml.Contains("youtube"))
            .Select(h => new { h.MatchId, h.Type })
            .ToListAsync(ct);

        var hasShort = existing.Where(e => e.Type == HighlightType.Short).Select(e => e.MatchId).ToHashSet();
        var hasHighlight = existing.Where(e => e.Type == HighlightType.Highlight).Select(e => e.MatchId).ToHashSet();

        var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");
        var saved = 0;

        foreach (var match in matches)
        {
            if (saved >= 20) break;
            var home = Normalize(match.HomeName);
            var away = Normalize(match.AwayName);

            // Fetch short goal clip if missing
            if (!hasShort.Contains(match.Id))
            {
                var vid = await ScrapeFirstVideoId(http,
                    $"{home} vs {away} goal Premier League", "&sp=EgIYAQ%253D%253D", ct);
                if (vid is not null)
                {
                    db.MatchHighlights.Add(new MatchHighlight
                    {
                        MatchId = match.Id, Title = "Goal", Type = HighlightType.Short,
                        EmbedHtml = BuildEmbed(vid)
                    });
                    saved++;
                }
            }

            // Fetch full highlights if missing
            if (!hasHighlight.Contains(match.Id))
            {
                var vid = await ScrapeFirstVideoId(http,
                    $"{home} vs {away} highlights Premier League", "", ct);
                if (vid is not null)
                {
                    db.MatchHighlights.Add(new MatchHighlight
                    {
                        MatchId = match.Id, Title = "Highlights", Type = HighlightType.Highlight,
                        EmbedHtml = BuildEmbed(vid)
                    });
                    saved++;
                }
            }

            await Task.Delay(500, ct);
        }

        if (saved > 0)
        {
            await uow.SaveChangesAsync(nameof(CacheHighlightsBackgroundService), ct);
            logger.LogInformation("Cached {Count} YouTube videos (shorts + highlights)", saved);
        }
    }

    private async Task<string?> ScrapeFirstVideoId(HttpClient http, string query, string sp, CancellationToken ct)
    {
        try
        {
            var html = await http.GetStringAsync(
                $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(query)}{sp}", ct);
            foreach (var m in VideoIdRegex().Matches(html).Cast<System.Text.RegularExpressions.Match>())
            {
                var vid = m.Groups[1].Value;
                try
                {
                    // oEmbed check — fails for private/unavailable videos
                    var resp = await http.GetAsync(
                        $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={vid}&format=json", ct);
                    if (!resp.IsSuccessStatusCode) continue;

                    // Verify page doesn't contain private/unavailable markers
                    var page = await http.GetStringAsync($"https://www.youtube.com/watch?v={vid}", ct);
                    if (page.Contains("\"isPrivate\":true") || page.Contains("\"status\":\"ERROR\""))
                        continue;

                    return vid;
                }
                catch { /* skip */ }
            }
            return null;
        }
        catch { return null; }
    }

    private async Task PurgeUnavailableAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IScoreCastDbContext>();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var http = httpClientFactory.CreateClient();
            http.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

            var all = await db.MatchHighlights.ToListAsync(ct);
            var removed = 0;

            foreach (var h in all)
            {
                var vidMatch = EmbedVideoIdRegex().Match(h.EmbedHtml);
                if (!vidMatch.Success) continue;
                var vid = vidMatch.Groups[1].Value;

                try
                {
                    var resp = await http.GetAsync(
                        $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={vid}&format=json", ct);
                    if (!resp.IsSuccessStatusCode)
                    {
                        db.MatchHighlights.Remove(h);
                        removed++;
                        continue;
                    }

                    var page = await http.GetStringAsync($"https://www.youtube.com/watch?v={vid}", ct);
                    if (page.Contains("\"isPrivate\":true") || page.Contains("\"status\":\"ERROR\""))
                    {
                        db.MatchHighlights.Remove(h);
                        removed++;
                    }
                }
                catch
                {
                    db.MatchHighlights.Remove(h);
                    removed++;
                }

                await Task.Delay(300, ct);
            }

            if (removed > 0)
            {
                await uow.SaveChangesAsync(nameof(CacheHighlightsBackgroundService), ct);
                logger.LogInformation("Purged {Count} unavailable/private highlights", removed);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Purge unavailable highlights failed");
        }
    }

    [GeneratedRegex("embed/([^?'\"]+)")]
    private static partial Regex EmbedVideoIdRegex();

    private static string BuildEmbed(string vid) =>
        $"<iframe src='https://www.youtube-nocookie.com/embed/{vid}?autoplay=1&amp;mute=0&amp;modestbranding=1&amp;rel=0&amp;iv_load_policy=3&amp;playsinline=1&amp;controls=1' frameborder='0' allowfullscreen allow='autoplay; fullscreen; encrypted-media' style='width:100%;height:100%;'></iframe>";

    private static string Normalize(string name) =>
        name.Replace(" FC", "").Replace(" AFC", "").Trim();

    [GeneratedRegex("\"videoId\":\"([^\"]+)\"")]
    private static partial Regex VideoIdRegex();
}
