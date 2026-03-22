using System.Net.Http.Json;
using System.Text.Json.Serialization;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Football.Commands;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Infrastructure.V1.Football.CommandHandlers;

internal sealed record GetMatchHighlightsCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork,
    IHttpClientFactory HttpClientFactory) : ICommandHandler<GetMatchHighlightsCommand, ScoreCastResponse<MatchHighlightsResult>>
{
    private static readonly MatchHighlightsResult Empty = new([]);

    public async Task<ScoreCastResponse<MatchHighlightsResult>> ExecuteAsync(
        GetMatchHighlightsCommand query, CancellationToken ct)
    {
        // Check DB cache first
        var cached = await DbContext.MatchHighlights
            .AsNoTracking()
            .Where(h => h.MatchId == query.MatchId)
            .Select(h => new HighlightVideo(h.Title, h.EmbedHtml))
            .ToListAsync(ct);

        if (cached.Count > 0)
            return ScoreCastResponse<MatchHighlightsResult>.Ok(new MatchHighlightsResult(cached));

        // Fetch match info for name matching
        var match = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Id == query.MatchId)
            .Select(m => new
            {
                HomeName = m.HomeTeam.Name,
                AwayName = m.AwayTeam.Name,
                HomeShort = m.HomeTeam.ShortName,
                AwayShort = m.AwayTeam.ShortName
            })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return ScoreCastResponse<MatchHighlightsResult>.Ok(Empty);

        // Fetch Scorebat feed
        List<ScoreBatMatch> feed;
        try
        {
            var http = HttpClientFactory.CreateClient();
            feed = await http.GetFromJsonAsync<List<ScoreBatMatch>>(
                "https://www.scorebat.com/video-api/v1/", ct) ?? [];
        }
        catch
        {
            return ScoreCastResponse<MatchHighlightsResult>.Ok(Empty);
        }

        // Find matching entry
        var found = feed.FirstOrDefault(m =>
                NameMatch(m.Side1?.Name, match.HomeName, match.HomeShort)
                && NameMatch(m.Side2?.Name, match.AwayName, match.AwayShort))
            ?? feed.FirstOrDefault(m =>
                NameMatch(m.Side1?.Name, match.AwayName, match.AwayShort)
                && NameMatch(m.Side2?.Name, match.HomeName, match.HomeShort));

        if (found is null || found.Videos.Count == 0)
            return ScoreCastResponse<MatchHighlightsResult>.Ok(Empty);

        // Cache in DB
        foreach (var v in found.Videos.Where(v => v.Embed is not null))
        {
            DbContext.MatchHighlights.Add(new MatchHighlight
            {
                MatchId = query.MatchId,
                Title = v.Title ?? "Highlights",
                EmbedHtml = v.Embed!
            });
        }

        await UnitOfWork.SaveChangesAsync(nameof(GetMatchHighlightsCommand), ct);

        var videos = found.Videos
            .Where(v => v.Embed is not null)
            .Select(v => new HighlightVideo(v.Title ?? "Highlights", v.Embed!))
            .ToList();

        return ScoreCastResponse<MatchHighlightsResult>.Ok(new MatchHighlightsResult(videos));
    }

    private static bool NameMatch(string? scorebatName, string dbName, string? shortName)
    {
        if (string.IsNullOrEmpty(scorebatName)) return false;
        var sb = Normalize(scorebatName);
        var db = Normalize(dbName);
        if (sb.Contains(db) || db.Contains(sb)) return true;
        if (shortName is not null)
        {
            var sn = Normalize(shortName);
            if (sb.Contains(sn) || sn.Contains(sb)) return true;
        }
        return false;
    }

    private static string Normalize(string name) =>
        name.ToLowerInvariant().Replace(" fc", "").Replace(" afc", "").Trim();
}

file class ScoreBatMatch
{
    [JsonPropertyName("side1")] public ScoreBatSide? Side1 { get; set; }
    [JsonPropertyName("side2")] public ScoreBatSide? Side2 { get; set; }
    [JsonPropertyName("videos")] public List<ScoreBatVideo> Videos { get; set; } = [];
}

file class ScoreBatSide
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}

file class ScoreBatVideo
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("embed")] public string? Embed { get; set; }
}
