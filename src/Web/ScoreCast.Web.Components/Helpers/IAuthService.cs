namespace ScoreCast.Web.Components.Helpers;

public interface IAuthService
{
    Task LogoutAsync();
    Task<(bool Success, string? Error)> RegisterAsync(string email, string username, string password);
}
