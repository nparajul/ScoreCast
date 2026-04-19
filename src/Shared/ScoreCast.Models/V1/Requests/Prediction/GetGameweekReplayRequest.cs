using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Prediction;

public record GetGameweekReplayRequest : ScoreCastRequest
{
    public long SeasonId { get; init; }
    public int GameweekNumber { get; init; }
    public long TargetUserId { get; init; }
}
