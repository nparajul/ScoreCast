namespace ScoreCast.Models.V1.Requests.Prediction;

public record CalculatePredictionPointsRequest : ScoreCastRequest
{
    public long SeasonId { get; set; }
}
