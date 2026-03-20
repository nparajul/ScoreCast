namespace ScoreCast.Models.V1.Requests.Prediction;

public record GetMyRiskPlaysRequest : ScoreCastRequest
{
    public long SeasonId { get; init; }
    public long GameweekId { get; init; }
}
