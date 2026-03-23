using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetAllHighlightsEndpoint : Endpoint<GetAllHighlightsRequest, ScoreCastResponse<AllHighlightsResult>>
{
    public override void Configure()
    {
        Get("/highlights");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(GetAllHighlightsRequest request, CancellationToken ct)
    {
        var result = await new GetAllHighlightsQuery(request.Skip, request.Take).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
