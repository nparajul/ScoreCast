using Refit;
using ScoreCast.Models.V1.Requests.Auth;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Auth;

namespace ScoreCast.ApiClient.V1.Apis;

public interface IAuthApi
{
    [Post("/api/v1/auth/token")]
    Task<ScoreCastResponse<TokenProxyResult>> TokenAsync([Body] object request, CancellationToken ct = default);

    [Post("/api/v1/auth/register")]
    Task<ScoreCastResponse<TokenProxyResult>> RegisterAsync([Body] RegisterRequest request, CancellationToken ct = default);
}
