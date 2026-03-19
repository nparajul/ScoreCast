namespace ScoreCast.Models.V1.Requests.Prediction;

public record EnrollUserSeasonRequest : ScoreCastRequest
{
    public long SeasonId { get; set; }
}
