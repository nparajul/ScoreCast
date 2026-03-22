using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

namespace ScoreCast.Ws.Endpoints.V1.Community;

public sealed class CommunityGroup : Group
{
    public CommunityGroup()
    {
        Configure("community", ep =>
        {
            ep.Description(x => x.WithTags("Community"));
            ep.Options(b => b.RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                .RequireAuthenticatedUser()));
        });
    }
}
