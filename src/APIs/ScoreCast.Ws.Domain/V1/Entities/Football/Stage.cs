using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Stage : ScoreCastEntity
{
    public long SeasonId { get; set; }
    public required string Name { get; set; }
    public StageType StageType { get; set; } = StageType.League;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Season Season { get; init; } = null!;
    public ICollection<MatchGroup> MatchGroups { get; init; } = [];
    public ICollection<Gameweek> Gameweeks { get; init; } = [];
}
