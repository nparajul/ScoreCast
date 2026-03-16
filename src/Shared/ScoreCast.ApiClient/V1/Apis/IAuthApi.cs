using Refit;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.ApiClient.V1.Apis;

public interface IAuthApi
{
    [Post("/api/v1/auth/token")]
    Task<ScoreCastResponse<string>> TokenAsync([Body] object request, CancellationToken ct = default);
}
