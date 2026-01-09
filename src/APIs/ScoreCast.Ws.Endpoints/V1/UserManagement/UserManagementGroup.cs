using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using ScoreCast.Shared.Constants;

namespace ScoreCast.Ws.Endpoints.V1.UserManagement;

public sealed class UserManagementGroup : Group
{
    public UserManagementGroup()
    {
        Configure("users", ep =>
        {
            ep.Description(x => x.WithTags("User Management"));
            ep.Options(b => b.RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme,
                    ApiKeyAuth.SchemeName)
                .RequireAuthenticatedUser()));
        });
    }
}
