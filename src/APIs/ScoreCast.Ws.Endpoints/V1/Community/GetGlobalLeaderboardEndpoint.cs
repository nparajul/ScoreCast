using ScoreCast.Models.V1.Requests.Community;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Community;

public sealed class GetGlobalLeaderboardEndpoint : Endpoint<GetGlobalLeaderboardRequest, ScoreCastResponse<GlobalLeaderboardResult>>
{
    public override void Configure()
    {
        Get("/leaderboard");
        Group<CommunityGroup>();
    }

    public override async Task HandleAsync(GetGlobalLeaderboardRequest req, CancellationToken ct)
    {
        var result = await new GetGlobalLeaderboardQuery(req.Competition).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
