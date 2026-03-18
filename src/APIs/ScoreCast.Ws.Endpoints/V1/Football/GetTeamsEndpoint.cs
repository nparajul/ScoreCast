using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetTeamsEndpoint : Endpoint<GetTeamsRequest, ScoreCastResponse<List<TeamResult>>>
{
    public override void Configure()
    {
        Get("/competitions/{CompetitionName}/teams");
        Group<FootballGroup>();
        Summary(s =>
        {
            s.Summary = "Get Teams by Competition";
            s.Description = "Returns all active teams for the current season of a given competition";
        });
    }

    public override async Task HandleAsync(GetTeamsRequest req, CancellationToken ct)
    {
        var result = await new GetTeamsQuery(req.CompetitionName).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
