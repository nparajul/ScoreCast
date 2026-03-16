namespace ScoreCast.Web.Auth;

public sealed record TokenResponse
{
    public string AccessToken { get; init; } = "";
    public string RefreshToken { get; init; } = "";
    public int ExpiresIn { get; init; }
    public int RefreshExpiresIn { get; init; }
}

public sealed record AuthResult(bool Success, string? Error = null);
