using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record CompetitionZone : ScoreCastEntity
{
    public required long CompetitionId { get; set; }
    public required string Name { get; set; }
    public required string Color { get; set; }
    public required int StartPosition { get; set; }
    public required int EndPosition { get; set; }
    public int SortOrder { get; set; }

    public Competition Competition { get; init; } = null!;
}
