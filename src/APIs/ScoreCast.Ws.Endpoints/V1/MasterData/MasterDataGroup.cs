using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using ScoreCast.Shared.Constants;

namespace ScoreCast.Ws.Endpoints.V1.MasterData;

public sealed class MasterDataGroup : Group
{
    public MasterDataGroup()
    {
        Configure("master-data", ep =>
        {
            ep.Description(x => x.WithTags("MasterData"));
            ep.Options(b => b.RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme,
                    ApiKeyAuth.SchemeName)
                .RequireAuthenticatedUser()));
        });
    }
}
