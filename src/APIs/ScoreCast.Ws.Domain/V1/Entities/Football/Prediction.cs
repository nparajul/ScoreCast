using ScoreCast.Ws.Domain.V1.Entities.Common;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Domain.V1.Entities.Football;

public sealed record Prediction : ScoreCastEntity
{
    public required long UserId { get; set; }
    public required long MatchId { get; set; }
    public required int PredictedHomeScore { get; set; }
    public required int PredictedAwayScore { get; set; }
    public int PointsAwarded { get; set; }

    public UserMaster User { get; init; } = default!;
    public Match Match { get; init; } = default!;
}
