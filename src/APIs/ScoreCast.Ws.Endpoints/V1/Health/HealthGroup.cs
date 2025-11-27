using Microsoft.AspNetCore.Http;

namespace ScoreCast.Ws.Endpoints.V1.Health;

public sealed class HealthGroup : Group
{
    public HealthGroup()
    {
        Configure("health", ep =>
        {
            ep.AllowAnonymous();
            ep.Description(x=>x.WithTags("Health Checks"));
        });
    }
}
