namespace ScoreCast.Ws.Endpoints.V1.Insights;

public sealed record GetMatchInsightsRequest
{
    [QueryParam] public long SeasonId { get; init; }
    [QueryParam] public int GameweekNumber { get; init; }
}
