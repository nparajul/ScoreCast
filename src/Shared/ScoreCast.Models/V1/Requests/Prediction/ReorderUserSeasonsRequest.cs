namespace ScoreCast.Models.V1.Requests.Prediction;

public record ReorderUserSeasonsRequest : ScoreCastRequest
{
    public List<long> SeasonIds { get; init; } = [];
}
