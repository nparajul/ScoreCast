using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record MatchHighlight : ScoreCastEntity
{
    public long MatchId { get; set; }
    public required string Title { get; set; }
    public required string EmbedHtml { get; set; }
    public HighlightType Type { get; set; } = HighlightType.Highlight;

    public Match Match { get; init; } = null!;
}
