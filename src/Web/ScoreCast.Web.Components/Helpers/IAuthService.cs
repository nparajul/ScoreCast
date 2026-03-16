namespace ScoreCast.Web.Components.Helpers;

public interface IAuthService
{
    Task LogoutAsync();
    string GetRegistrationUrl(string returnUrl);
}
