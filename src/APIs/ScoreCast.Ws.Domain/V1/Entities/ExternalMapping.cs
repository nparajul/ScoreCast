using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Domain.V1.Enums;

namespace ScoreCast.Ws.Domain.V1.Entities;

public sealed record ExternalMapping : ScoreCastEntity
{
    public required EntityType EntityType { get; set; }
    public required long EntityId { get; set; }
    public required ExternalSource Source { get; set; }
    public required string ExternalCode { get; set; }
}
