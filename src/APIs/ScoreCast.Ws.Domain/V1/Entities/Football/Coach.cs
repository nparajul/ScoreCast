using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Coach : ScoreCastEntity
{
    public required string Name { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? PhotoUrl { get; set; }
    public string? ExternalId { get; set; }
    public DateOnly? ValidFrom { get; set; }
}
