using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Shared.Enums;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Competition : ScoreCastEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public long CountryId { get; set; }
    public string? LogoUrl { get; set; }
    public string? ExternalId { get; set; }
    public LeagueType Type { get; set; } = LeagueType.League;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Country Country { get; init; } = null!;
    public ICollection<Season> Seasons { get; init; } = [];
}
