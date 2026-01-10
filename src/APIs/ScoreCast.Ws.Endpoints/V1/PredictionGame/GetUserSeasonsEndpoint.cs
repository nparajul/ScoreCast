using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class GetUserSeasonsEndpoint : Endpoint<GetUserSeasonsRequest, ScoreCastResponse<List<UserSeasonResult>>>
{
    public override void Configure()
    {
        Get("/user-seasons");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(GetUserSeasonsRequest request, CancellationToken ct)
    {
        var result = await new GetUserSeasonsQuery(request).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
