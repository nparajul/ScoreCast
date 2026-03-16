using Microsoft.AspNetCore.Http;

namespace ScoreCast.Ws.Endpoints.V1.Auth;

public sealed class AuthGroup : Group
{
    public AuthGroup()
    {
        Configure("auth", ep =>
        {
            ep.AllowAnonymous();
            ep.Description(x => x.WithTags("Authentication"));
        });
    }
}
