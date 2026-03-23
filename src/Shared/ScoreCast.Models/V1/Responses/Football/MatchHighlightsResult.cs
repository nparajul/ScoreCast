namespace ScoreCast.Models.V1.Responses.Football;

public record MatchHighlightsResult(List<HighlightVideo> Videos);

public record HighlightVideo(string Title, string EmbedHtml);
