namespace ScoreCast.Models.V1.Requests.Insights;

public record GetMatchInsightsRequest : ScoreCastRequest
{
    public long SeasonId { get; set; }
    public int GameweekNumber { get; set; }
}
