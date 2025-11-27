using Refit;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.ApiClient.V1.Apis;

public partial interface IHealthApi
{
    [Get("/api/v1/health")]
    Task<ScoreCastResponse> CheckHealthAsync();
}
