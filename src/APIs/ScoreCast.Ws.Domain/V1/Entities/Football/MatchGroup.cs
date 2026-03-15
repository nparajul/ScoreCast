using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record MatchGroup : ScoreCastEntity
{
    public long StageId { get; set; }
    public required string Name { get; set; }
    public int SortOrder { get; set; }

    public Stage Stage { get; init; } = null!;
    public ICollection<Match> Matches { get; init; } = [];
}
