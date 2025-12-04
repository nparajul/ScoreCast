using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Prediction;

public record JoinPredictionLeagueRequest : ScoreCastRequest
{
    public required string InviteCode { get; set; }
}
