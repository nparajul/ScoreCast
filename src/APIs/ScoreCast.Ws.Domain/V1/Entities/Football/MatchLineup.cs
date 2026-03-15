using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record MatchLineup : ScoreCastEntity
{
    public long MatchId { get; set; }
    public long PlayerId { get; set; }
    public bool IsStarter { get; set; }

    public Match Match { get; init; } = null!;
    public Player Player { get; init; } = null!;
}
