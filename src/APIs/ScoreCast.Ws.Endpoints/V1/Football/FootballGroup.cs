using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using ScoreCast.Shared.Constants;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class FootballGroup : Group
{
    public FootballGroup()
    {
        Configure("football", ep =>
        {
            ep.Description(x => x.WithTags("Football"));
            ep.Options(b => b.RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme,
                    ApiKeyAuth.SchemeName)
                .RequireAuthenticatedUser()));
        });
    }
}
