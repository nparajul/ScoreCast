using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using ScoreCast.Shared.Constants;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class PredictionGroup : Group
{
    public PredictionGroup()
    {
        Configure("prediction", ep =>
        {
            ep.Description(x => x.WithTags("Prediction"));
            ep.Options(b => b.RequireAuthorization(policy =>
                policy.AddAuthenticationSchemes(
                    JwtBearerDefaults.AuthenticationScheme,
                    ApiKeyAuth.SchemeName)
                .RequireAuthenticatedUser()));
        });
    }
}
