using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Prediction;

public record GetLeagueStandingsRequest : ScoreCastRequest
{
    public long PredictionLeagueId { get; set; }
}
