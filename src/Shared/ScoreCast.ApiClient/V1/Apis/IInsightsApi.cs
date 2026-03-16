using Refit;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.ApiClient.V1.Apis;

public partial interface IScoreCastApiClient
{
    [Get("/api/v1/insights/upcoming")]
    Task<ScoreCastResponse<List<MatchInsightResult>>> GetMatchInsightsAsync(long seasonId, int gameweekNumber, CancellationToken ct);
}
