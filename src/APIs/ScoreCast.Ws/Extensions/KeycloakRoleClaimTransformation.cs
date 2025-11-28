using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace ScoreCast.Ws.Extensions;

internal sealed class KeycloakRoleClaimTransformation : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null || !identity.IsAuthenticated)
            return Task.FromResult(principal);

        var roleClaims = identity.FindAll("roles").ToList();
        foreach (var roleClaim in roleClaims)
        {
            if (!identity.HasClaim(ClaimTypes.Role, roleClaim.Value))
                identity.AddClaim(new Claim(ClaimTypes.Role, roleClaim.Value));
        }

        return Task.FromResult(principal);
    }
}
