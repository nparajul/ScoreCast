using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Prediction;

public record GetPredictionReplayCardRequest : ScoreCastRequest
{
    public long MatchId { get; init; }
    public long TargetUserId { get; init; }
}
