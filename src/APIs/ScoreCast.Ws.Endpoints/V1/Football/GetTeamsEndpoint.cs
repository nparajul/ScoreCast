using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetTeamsEndpoint : EndpointWithoutRequest<ScoreCastResponse<List<TeamResult>>>
{
    public override void Configure()
    {
        Get("/competitions/{competitionName}/teams");
        Group<FootballGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Teams by Competition";
            s.Description = "Returns all active teams for the current season of a given competition";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var competitionName = Route<string>("competitionName")!;
        var result = await new GetTeamsQuery(competitionName).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
