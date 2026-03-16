using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;

namespace ScoreCast.Ws.Endpoints.V1.Insights;

public sealed class InsightsGroup : Group
{
    public InsightsGroup()
    {
        Configure("insights", ep =>
        {
            ep.Description(x => x.WithTags("Insights"));
            ep.Options(b => b.RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                    .RequireAuthenticatedUser()));
        });
    }
}
