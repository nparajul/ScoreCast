using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Community;

public sealed class GetGlobalLeaderboardEndpoint : EndpointWithoutRequest<ScoreCastResponse<GlobalLeaderboardResult>>
{
    public override void Configure()
    {
        Get("/leaderboard");
        Group<CommunityGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await new GetGlobalLeaderboardQuery().ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
