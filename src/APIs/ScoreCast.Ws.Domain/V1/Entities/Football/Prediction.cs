using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Prediction : ScoreCastEntity
{
    public long SeasonId { get; set; }
    public long UserId { get; set; }
    public long MatchId { get; set; }
    public int PredictedHomeScore { get; set; }
    public int PredictedAwayScore { get; set; }
    public PredictionOutcome? Outcome { get; set; }

    public Season Season { get; init; } = null!;
    public UserMaster User { get; init; } = null!;
    public Match Match { get; init; } = null!;
}
