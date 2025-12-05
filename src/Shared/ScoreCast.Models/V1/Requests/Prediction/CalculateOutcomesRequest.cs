namespace ScoreCast.Models.V1.Requests.Prediction;

public record CalculateOutcomesRequest : ScoreCastRequest
{
    public long SeasonId { get; set; }
}
