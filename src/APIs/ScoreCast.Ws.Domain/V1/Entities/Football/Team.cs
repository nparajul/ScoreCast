using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Team : ScoreCastEntity
{
    public required string Name { get; set; }
    public string? ShortName { get; set; }
    public string? LogoUrl { get; set; }
    public string? ExternalId { get; set; }
    public long CountryId { get; set; }
    public int? Founded { get; set; }
    public string? Venue { get; set; }
    public string? ClubColors { get; set; }
    public string? Website { get; set; }
    public bool IsActive { get; set; } = true;

    public Country Country { get; init; } = default!;
    public ICollection<SeasonTeam> SeasonTeams { get; init; } = [];
}
