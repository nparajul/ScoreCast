using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.League;

public sealed record LeagueMaster : ScoreCastEntity
{
    public required string Name { get; set; }
    public required long CountryId { get; set; }
    public string? LogoUrl { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public CountryMaster Country { get; init; } = default!;
    public ICollection<TeamMaster> Teams { get; init; } = [];
}
