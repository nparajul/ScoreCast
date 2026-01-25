using ScoreCast.Shared.Enums;

namespace ScoreCast.Models.V1.Requests.Prediction;

public record SubmitRiskPlaysRequest : ScoreCastRequest
{
    public long SeasonId { get; init; }
    public List<RiskPlayEntry> RiskPlays { get; init; } = [];
}

public record RiskPlayEntry
{
    public long MatchId { get; init; }
    public RiskPlayType RiskType { get; init; }
    public string? Selection { get; init; }
}
