using System.Net.Http.Json;
using System.Text.Json.Serialization;
using ScoreCast.Shared.Types;

namespace ScoreCast.Web.Components.Helpers;

public interface IScoreBatService
{
    Task<List<ScoreBatVideo>> GetHighlightsAsync(string homeTeam, string awayTeam, CancellationToken ct = default);
}

public class ScoreBatService(HttpClient http) : IScoreBatService
{
    private List<ScoreBatMatch>? _cache;
    private DateTime _cacheTime;

    public async Task<List<ScoreBatVideo>> GetHighlightsAsync(string homeTeam, string awayTeam, CancellationToken ct = default)
    {
        // Cache feed for 5 minutes
        if (_cache is null || ScoreCastDateTime.Now.Value - _cacheTime > TimeSpan.FromMinutes(5))
        {
            try
            {
                _cache = await http.GetFromJsonAsync<List<ScoreBatMatch>>(
                    "https://www.scorebat.com/video-api/v1/", ct) ?? [];
                _cacheTime = ScoreCastDateTime.Now.Value;
            }
            catch
            {
                return [];
            }
        }

        var match = _cache.FirstOrDefault(m =>
            FuzzyMatch(m.Side1?.Name, homeTeam, awayTeam) && FuzzyMatch(m.Side2?.Name, awayTeam, homeTeam));

        // Try reversed (Scorebat doesn't always match home/away order)
        match ??= _cache.FirstOrDefault(m =>
            FuzzyMatch(m.Side1?.Name, awayTeam, homeTeam) && FuzzyMatch(m.Side2?.Name, homeTeam, awayTeam));

        return match?.Videos ?? [];
    }

    private static bool FuzzyMatch(string? scorebatName, string primary, string secondary)
    {
        if (string.IsNullOrEmpty(scorebatName)) return false;
        var sb = scorebatName.ToLowerInvariant();
        var p = primary.ToLowerInvariant();
        var s = secondary.ToLowerInvariant();
        return sb.Contains(p) || p.Contains(sb)
            || sb.Contains(s) || s.Contains(sb);
    }
}

public class ScoreBatMatch
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("thumbnail")] public string? Thumbnail { get; set; }
    [JsonPropertyName("date")] public string? Date { get; set; }
    [JsonPropertyName("side1")] public ScoreBatSide? Side1 { get; set; }
    [JsonPropertyName("side2")] public ScoreBatSide? Side2 { get; set; }
    [JsonPropertyName("competition")] public ScoreBatCompetition? Competition { get; set; }
    [JsonPropertyName("videos")] public List<ScoreBatVideo> Videos { get; set; } = [];
}

public class ScoreBatSide
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public class ScoreBatCompetition
{
    [JsonPropertyName("name")] public string? Name { get; set; }
}

public class ScoreBatVideo
{
    [JsonPropertyName("title")] public string? Title { get; set; }
    [JsonPropertyName("embed")] public string? Embed { get; set; }
}
