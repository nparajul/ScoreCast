namespace ScoreCast.Models.V1.Requests.Prediction;

public record CreatePredictionLeagueRequest : ScoreCastRequest
{
    public required string Name { get; set; }
    public long CompetitionId { get; set; }
}
