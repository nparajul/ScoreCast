using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetMatchPageEndpoint : Endpoint<GetMatchPageRequest, ScoreCastResponse<MatchPageResult>>
{
    public override void Configure()
    {
        Get("/matches/{MatchId}");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(GetMatchPageRequest request, CancellationToken ct)
    {
        var result = await new GetMatchPageQuery(request.MatchId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
