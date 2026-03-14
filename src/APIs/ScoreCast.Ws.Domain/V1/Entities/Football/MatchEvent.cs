using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Domain.V1.Enums;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record MatchEvent : ScoreCastEntity
{
    public required long MatchId { get; set; }
    public required long PlayerId { get; set; }
    public required MatchEventType EventType { get; set; }
    public int Value { get; set; } = 1;

    public Match Match { get; init; } = default!;
    public Player Player { get; init; } = default!;
}
