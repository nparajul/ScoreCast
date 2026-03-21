using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetMatchExtrasEndpoint : Endpoint<GetMatchExtrasRequest, ScoreCastResponse<MatchExtrasResult>>
{
    public override void Configure()
    {
        Get("/matches/{MatchId}/extras");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(GetMatchExtrasRequest request, CancellationToken ct)
    {
        var result = await new GetMatchExtrasQuery(request.MatchId, request.UserId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
