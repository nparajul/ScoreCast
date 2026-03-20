namespace ScoreCast.Models.V1.Responses.Football;

public record AllHighlightsResult(List<HighlightItem> Items, bool HasMore);

public record HighlightItem(
    long MatchId,
    string HomeTeam,
    string AwayTeam,
    string? HomeShortName,
    string? AwayShortName,
    string? HomeLogoUrl,
    string? AwayLogoUrl,
    int? HomeScore,
    int? AwayScore,
    DateTime? KickoffTime,
    string CompetitionName,
    string Title,
    string EmbedHtml);
