using Refit;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.ApiClient.V1.Apis;

public partial interface IScoreCastApiClient
{
    [Get("/api/v1/community/dashboard")]
    Task<ScoreCastResponse<GlobalDashboardResult>> GetGlobalDashboardAsync([Query] string? competition, CancellationToken ct);

    [Get("/api/v1/community/leaderboard")]
    Task<ScoreCastResponse<GlobalLeaderboardResult>> GetGlobalLeaderboardAsync([Query] string? competition, CancellationToken ct);
}
