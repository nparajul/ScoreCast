using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetTeamPlayerStatsEndpoint : Endpoint<GetTeamPlayerStatsRequest, ScoreCastResponse<PlayerStatsResult>>
{
    public override void Configure()
    {
        Get("/teams/{TeamId}/player-stats");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(GetTeamPlayerStatsRequest request, CancellationToken ct)
    {
        var result = await new GetTeamPlayerStatsQuery(request.TeamId, request.SeasonId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
