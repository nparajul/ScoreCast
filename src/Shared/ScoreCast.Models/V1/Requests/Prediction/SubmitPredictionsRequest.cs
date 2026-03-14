using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Prediction;

public record SubmitPredictionsRequest : ScoreCastRequest
{
    public long SeasonId { get; set; }
    public List<PredictionEntry> Predictions { get; set; } = [];
}

public record PredictionEntry
{
    public long MatchId { get; set; }
    public int PredictedHomeScore { get; set; }
    public int PredictedAwayScore { get; set; }
}
