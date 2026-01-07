using ScoreCast.Models.V1.Requests;
using ScoreCast.Models.V1.Responses;

namespace ScoreCast.Ws.Endpoints.V1.Auth;

public sealed record TokenProxyRequest : ScoreCastRequest
{
    public string GrantType { get; init; } = "";
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? RefreshToken { get; init; }
}
