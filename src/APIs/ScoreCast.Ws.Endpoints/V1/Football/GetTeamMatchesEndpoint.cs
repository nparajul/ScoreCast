using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetTeamMatchesEndpoint : Endpoint<GetTeamMatchesRequest, ScoreCastResponse<TeamMatchesResult>>
{
    public override void Configure()
    {
        Get("/teams/{TeamId}/matches");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(GetTeamMatchesRequest request, CancellationToken ct)
    {
        var result = await new GetTeamMatchesQuery(request.TeamId, request.SeasonId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
