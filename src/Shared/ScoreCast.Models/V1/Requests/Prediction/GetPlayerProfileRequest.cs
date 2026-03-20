namespace ScoreCast.Models.V1.Requests.Prediction;

public record GetPlayerProfileRequest : ScoreCastRequest
{
    public long TargetUserId { get; init; }
    public long PredictionLeagueId { get; init; }
}
