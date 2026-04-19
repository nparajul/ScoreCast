using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Prediction;

public record GetPredictionReplayRequest : ScoreCastRequest
{
    public long MatchId { get; init; }
    public long? PredictionLeagueId { get; init; }
}
