using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.League;

public sealed record CountryMaster : ScoreCastEntity
{
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? FlagUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
