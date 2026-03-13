using System.Security.Claims;

namespace ScoreCast.Ws.Infrastructure.Internal;

internal static class UserClaimsExtension
{
    private const string _guest = "Guest";

    public static string CurrentUser(this ClaimsPrincipal? principal)
    {
        if (principal == null || principal.Identity?.IsAuthenticated == false)
            return _guest;

        var user = principal.FindFirstValue("sub");
        if (!string.IsNullOrEmpty(user))
            return user;

        return !string.IsNullOrEmpty(principal.Identity?.Name)
            ? principal.Identity?.Name ?? _guest
            : principal.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value ?? _guest;
    }
}
