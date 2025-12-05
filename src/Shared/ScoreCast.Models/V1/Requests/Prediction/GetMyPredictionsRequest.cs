using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Prediction;

public record GetMyPredictionsRequest : ScoreCastRequest
{
    public long PredictionLeagueId { get; set; }
    public long GameweekId { get; set; }
}
