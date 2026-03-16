using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetTeamSquadEndpoint : Endpoint<GetTeamSquadRequest, ScoreCastResponse<TeamSquadResult>>
{
    public override void Configure()
    {
        Get("/teams/{TeamId}/squad");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(GetTeamSquadRequest request, CancellationToken ct)
    {
        var result = await new GetTeamSquadQuery(request.TeamId, request.SeasonId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
