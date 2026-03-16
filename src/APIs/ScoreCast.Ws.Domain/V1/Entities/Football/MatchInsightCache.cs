using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record MatchInsightCache : ScoreCastEntity
{
    public long SeasonId { get; set; }
    public int GameweekNumber { get; set; }
    public required string ResponseJson { get; set; }

    public Season Season { get; init; } = null!;
}
