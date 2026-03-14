using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Domain.V1.Entities.Common;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record PredictionScoringRule : ScoreCastEntity
{
    public PredictionType PredictionType { get; set; } = PredictionType.Score;
    public StageType? StageType { get; set; }
    public PredictionOutcome Outcome { get; set; }
    public int Points { get; set; }
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
