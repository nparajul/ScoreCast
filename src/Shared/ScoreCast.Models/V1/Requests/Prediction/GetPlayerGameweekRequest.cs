namespace ScoreCast.Models.V1.Requests.Prediction;

public record GetPlayerGameweekRequest : ScoreCastRequest
{
    public long TargetUserId { get; init; }
    public long SeasonId { get; init; }
    public long GameweekId { get; init; }
    public long PredictionLeagueId { get; init; }
}
