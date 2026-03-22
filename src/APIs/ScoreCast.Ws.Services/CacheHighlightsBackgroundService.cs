using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Services;

public sealed class CacheHighlightsBackgroundService(
    IServiceScopeFactory scopeFactory,
    IHttpClientFactory httpClientFactory,
    ILogger<CacheHighlightsBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        await Task.Delay(TimeSpan.FromMinutes(1), ct);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CacheFromFeedAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "CacheHighlights background failed");
            }

            await Task.Delay(TimeSpan.FromHours(1), ct);
        }
    }

    private async Task CacheFromFeedAsync(CancellationToken ct)
    {
        var http = httpClientFactory.CreateClient();
        var feed = await http.GetFromJsonAsync<List<FeedMatch>>(
            "https://www.scorebat.com/video-api/v1/", ct);

        if (feed is null || feed.Count == 0) return;

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IScoreCastDbContext>();
        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        // Get all finished matches with team names
        var matches = await db.Matches
            .AsNoTracking()
            .Where(m => m.Status == Shared.Enums.MatchStatus.Finished)
            .Select(m => new
            {
                m.Id,
                HomeName = m.HomeTeam.Name,
                AwayName = m.AwayTeam.Name,
                HomeShort = m.HomeTeam.ShortName,
                AwayShort = m.AwayTeam.ShortName
            })
            .ToListAsync(ct);

        // Get already cached match IDs
        var cached = await db.MatchHighlights
            .AsNoTracking()
            .Select(h => h.MatchId)
            .Distinct()
            .ToListAsync(ct);

        var cachedSet = cached.ToHashSet();
        var saved = 0;

        foreach (var fm in feed)
        {
            var s1 = Normalize(fm.Side1?.Name);
            var s2 = Normalize(fm.Side2?.Name);
            if (s1 is null || s2 is null) continue;

            var match = matches.FirstOrDefault(m =>
                    NameMatch(s1, m.HomeName, m.HomeShort) && NameMatch(s2, m.AwayName, m.AwayShort))
                ?? matches.FirstOrDefault(m =>
                    NameMatch(s1, m.AwayName, m.AwayShort) && NameMatch(s2, m.HomeName, m.HomeShort));

            if (match is null || cachedSet.Contains(match.Id)) continue;

            if (fm.Embed is not null)
                db.MatchHighlights.Add(new MatchHighlight
                    { MatchId = match.Id, Title = "Goals", EmbedHtml = fm.Embed });

            foreach (var v in fm.Videos.Where(v => v.Embed is not null))
                db.MatchHighlights.Add(new MatchHighlight
                    { MatchId = match.Id, Title = v.Title ?? "Highlights", EmbedHtml = v.Embed! });

            cachedSet.Add(match.Id);
            saved++;
        }

        if (saved > 0)
        {
            await uow.SaveChangesAsync(nameof(CacheHighlightsBackgroundService), ct);
            logger.LogInformation("Cached highlights for {Count} matches", saved);
        }
    }

    private static bool NameMatch(string scorebat, string dbName, string? shortName)
    {
        var db = Normalize(dbName)!;
        if (scorebat.Contains(db) || db.Contains(scorebat)) return true;
        if (shortName is not null)
        {
            var sn = Normalize(shortName)!;
            if (scorebat.Contains(sn) || sn.Contains(scorebat)) return true;
        }
        return false;
    }

    private static string? Normalize(string? name) =>
        name?.ToLowerInvariant().Replace(" fc", "").Replace(" afc", "").Trim();
}

file class FeedMatch
{
    [JsonPropertyName("embed")] public string? Embed { get; set; }
    [JsonPropertyName("side1")] public FeedSide? Side1 { get; set; }
    [JsonPropertyName("side2")] public FeedSide? Side2 { get; set; }
    [JsonPropertyName("videos")] public List<FeedVideo> Videos { get; set; } = [];
}

file class FeedSide
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}

file class FeedVideo
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("embed")] public string? Embed { get; set; }
}
