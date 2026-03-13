using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.League;
using ScoreCast.Ws.Application.V1.League.Queries;

namespace ScoreCast.Ws.Endpoints.V1.League;

public sealed class GetTeamsEndpoint : EndpointWithoutRequest<ScoreCastResponse<List<TeamResult>>>
{
    public override void Configure()
    {
        Get("/{leagueName}/teams");
        Group<LeagueGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Teams by League";
            s.Description = "Returns all active teams for a given league";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var leagueName = Route<string>("leagueName")!;
        var result = await new GetTeamsQuery(leagueName).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
