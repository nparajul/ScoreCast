using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Community;

public sealed class GetGlobalDashboardEndpoint : EndpointWithoutRequest<ScoreCastResponse<GlobalDashboardResult>>
{
    public override void Configure()
    {
        Get("/dashboard");
        Group<CommunityGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await new GetGlobalDashboardQuery().ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
