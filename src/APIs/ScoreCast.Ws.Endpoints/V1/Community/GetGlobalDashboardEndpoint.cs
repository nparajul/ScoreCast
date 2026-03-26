using ScoreCast.Models.V1.Requests.Community;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Community;

public sealed class GetGlobalDashboardEndpoint : Endpoint<GetGlobalDashboardRequest, ScoreCastResponse<GlobalDashboardResult>>
{
    public override void Configure()
    {
        Get("/dashboard");
        Group<CommunityGroup>();
    }

    public override async Task HandleAsync(GetGlobalDashboardRequest req, CancellationToken ct)
    {
        var result = await new GetGlobalDashboardQuery(req.Competition).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
