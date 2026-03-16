using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetTeamDetailEndpoint : Endpoint<GetTeamDetailRequest, ScoreCastResponse<TeamDetailResult>>
{
    public override void Configure()
    {
        Get("/teams/{TeamId}");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(GetTeamDetailRequest request, CancellationToken ct)
    {
        var result = await new GetTeamDetailQuery(request.TeamId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
