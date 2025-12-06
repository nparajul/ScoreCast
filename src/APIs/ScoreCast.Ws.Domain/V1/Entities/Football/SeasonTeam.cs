using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record SeasonTeam : ScoreCastEntity
{
    public long SeasonId { get; set; }
    public long TeamId { get; set; }

    public Season Season { get; init; } = null!;
    public Team Team { get; init; } = null!;
}
