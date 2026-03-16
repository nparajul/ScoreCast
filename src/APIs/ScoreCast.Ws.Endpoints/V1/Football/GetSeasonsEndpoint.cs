using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetSeasonsEndpoint : EndpointWithoutRequest<ScoreCastResponse<List<SeasonResult>>>
{
    public override void Configure()
    {
        Get("/competitions/{competitionCode}/seasons");
        Group<FootballGroup>();
        Summary(s =>
        {
            s.Summary = "Get Seasons";
            s.Description = "Returns all seasons for a competition";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var code = Route<string>("competitionCode")!;
        var result = await new GetSeasonsQuery(code).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
