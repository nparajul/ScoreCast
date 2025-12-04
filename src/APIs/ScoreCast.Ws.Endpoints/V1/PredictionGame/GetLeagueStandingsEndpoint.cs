using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class GetLeagueStandingsEndpoint : Endpoint<GetLeagueStandingsRequest, ScoreCastResponse<LeagueStandingsResult>>
{
    public override void Configure()
    {
        Get("/leagues/{PredictionLeagueId}/standings");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(GetLeagueStandingsRequest request, CancellationToken ct)
    {
        var result = await new GetLeagueStandingsQuery(request.PredictionLeagueId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
