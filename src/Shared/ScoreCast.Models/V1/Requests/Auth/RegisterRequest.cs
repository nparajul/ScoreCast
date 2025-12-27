using ScoreCast.Models.V1.Requests;

namespace ScoreCast.Models.V1.Requests.Auth;

public sealed record RegisterRequest : ScoreCastRequest
{
    public string Email { get; init; } = "";
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
}
