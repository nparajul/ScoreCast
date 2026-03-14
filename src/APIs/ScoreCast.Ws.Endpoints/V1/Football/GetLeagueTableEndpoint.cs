using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetLeagueTableEndpoint : EndpointWithoutRequest<ScoreCastResponse<List<LeagueTableRow>>>
{
    public override void Configure()
    {
        Get("/seasons/{seasonId}/table");
        Group<FootballGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get League Table";
            s.Description = "Returns the league table for a given season, computed from match results";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var seasonId = Route<long>("seasonId");
        var result = await new GetLeagueTableQuery(seasonId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
