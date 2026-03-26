using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Services;

public sealed partial class CleanupHighlightsBackgroundService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<CleanupHighlightsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Highlights cleanup failed");
            }

            await Task.Delay(TimeSpan.FromHours(2), stoppingToken);
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IScoreCastDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var http = httpClientFactory.CreateClient();

        var highlights = await db.MatchHighlights
            .Where(h => !h.IsDeleted)
            .Select(h => new { h.Id, h.EmbedHtml })
            .ToListAsync(ct);

        var removed = 0;

        foreach (var h in highlights)
        {
            var videoId = ExtractVideoId(h.EmbedHtml);
            if (videoId is null) continue;

            try
            {
                var resp = await http.GetAsync(
                    $"https://www.youtube.com/oembed?url=https://www.youtube.com/watch?v={videoId}&format=json", ct);

                if (!resp.IsSuccessStatusCode)
                {
                    var entity = await db.MatchHighlights.FindAsync([h.Id], ct);
                    if (entity is not null) { entity.IsDeleted = true; removed++; }
                    continue;
                }

                // Check for copyright blocks / unavailable when embedded
                var page = await http.GetStringAsync($"https://www.youtube.com/watch?v={videoId}", ct);
                if (page.Contains("\"isPrivate\":true") ||
                    page.Contains("\"status\":\"ERROR\"") ||
                    page.Contains("blocked it on copyright grounds") ||
                    page.Contains("not available in your country") ||
                    page.Contains("This video is unavailable") ||
                    page.Contains("\"playabilityStatus\":{\"status\":\"ERROR\"") ||
                    page.Contains("\"playabilityStatus\":{\"status\":\"UNPLAYABLE\""))
                {
                    var entity = await db.MatchHighlights.FindAsync([h.Id], ct);
                    if (entity is not null) { entity.IsDeleted = true; removed++; }
                }
            }
            catch
            {
                // Network error — skip, don't delete
            }

            // Rate limit: don't hammer YouTube
            await Task.Delay(500, ct);
        }

        if (removed > 0)
        {
            await uow.SaveChangesAsync(nameof(CleanupHighlightsBackgroundService), ct);
            logger.LogInformation("Highlights cleanup: soft-deleted {Count} unavailable videos", removed);
        }
        else
        {
            logger.LogInformation("Highlights cleanup: all {Total} videos are valid", highlights.Count);
        }
    }

    private static string? ExtractVideoId(string embedHtml)
    {
        var match = VideoIdRegex().Match(embedHtml);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"embed/([a-zA-Z0-9_-]{11})")]
    private static partial Regex VideoIdRegex();
}
