namespace ScoreCast.Models.V1.Requests.Prediction;

public record GetMyPredictionsRequest : ScoreCastRequest
{
    public long SeasonId { get; set; }
    public long GameweekId { get; set; }
}
