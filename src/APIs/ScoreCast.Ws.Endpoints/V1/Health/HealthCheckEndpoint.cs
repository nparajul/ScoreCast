namespace ScoreCast.Ws.Endpoints.V1.Health;

public sealed class HealthCheckEndpoint : EndpointWithoutRequest<ScoreCastResponse>
{
    public override void Configure()
    {
        Get("/");
        Group<HealthGroup>();
        Summary(s =>
        {
            s.Summary = "Health Check";
            s.Description = "Returns API health status";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        await Send.OkAsync(ScoreCastResponse.Ok("ScoreCast API is healthy"), ct);
    }
}
