using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Country : ScoreCastEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? ExternalId { get; set; }
    public string? FlagUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
