using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetBracketEndpoint : Endpoint<GetBracketRequest, ScoreCastResponse<BracketResult>>
{
    public override void Configure()
    {
        Get("/seasons/{SeasonId}/bracket");
        Group<FootballGroup>();
        Summary(s =>
        {
            s.Summary = "Get Knockout Bracket";
            s.Description = "Returns the knockout bracket template merged with real match data for a season";
        });
    }

    public override async Task HandleAsync(GetBracketRequest request, CancellationToken ct)
    {
        var result = await new GetBracketQuery(request.SeasonId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
