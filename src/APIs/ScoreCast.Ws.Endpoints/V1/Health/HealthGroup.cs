namespace ScoreCast.Ws.Endpoints.V1.Health;

public sealed class HealthGroup : Group
{
    public HealthGroup()
    {
        Configure("api/v1/health", ep => { ep.AllowAnonymous(); });
    }
}
