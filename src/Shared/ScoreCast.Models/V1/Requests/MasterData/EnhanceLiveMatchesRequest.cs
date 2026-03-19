namespace ScoreCast.Models.V1.Requests.MasterData;

public record EnhanceLiveMatchesRequest : ScoreCastRequest
{
    public long? SeasonId { get; set; }
}
