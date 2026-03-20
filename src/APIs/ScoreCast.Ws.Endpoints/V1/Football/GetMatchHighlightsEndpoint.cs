using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Commands;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetMatchHighlightsEndpoint : Endpoint<GetMatchHighlightsRequest, ScoreCastResponse<MatchHighlightsResult>>
{
    public override void Configure()
    {
        Get("/matches/{MatchId}/highlights");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(GetMatchHighlightsRequest request, CancellationToken ct)
    {
        var result = await new GetMatchHighlightsCommand(request.MatchId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
