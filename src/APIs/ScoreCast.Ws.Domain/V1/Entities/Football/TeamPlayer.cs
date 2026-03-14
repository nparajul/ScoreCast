using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record TeamPlayer : ScoreCastEntity
{
    public long TeamId { get; set; }
    public long PlayerId { get; set; }
    public long SeasonId { get; set; }

    public Team Team { get; init; } = default!;
    public Player Player { get; init; } = default!;
    public Season Season { get; init; } = default!;
}
