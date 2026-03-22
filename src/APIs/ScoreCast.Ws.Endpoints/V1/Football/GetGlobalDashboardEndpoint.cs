using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetGlobalDashboardEndpoint : EndpointWithoutRequest<ScoreCastResponse<GlobalDashboardResult>>
{
    public override void Configure()
    {
        Get("/global-dashboard");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await new GetGlobalDashboardQuery().ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
