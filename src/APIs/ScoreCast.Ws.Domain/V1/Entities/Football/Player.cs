using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Player : ScoreCastEntity
{
    public required string Name { get; set; }
    public string? Position { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? ExternalId { get; set; }

    public ICollection<TeamPlayer> TeamPlayers { get; init; } = [];
}
