using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetMatchPredictionEndpoint : Endpoint<GetMatchPredictionRequest, ScoreCastResponse<MatchPredictionResult>>
{
    public override void Configure()
    {
        Get("/matches/{MatchId}/prediction");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(GetMatchPredictionRequest request, CancellationToken ct)
    {
        var result = await new GetMatchPredictionQuery(request.MatchId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
