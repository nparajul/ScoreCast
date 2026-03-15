using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetPointsTableEndpoint : EndpointWithoutRequest<ScoreCastResponse<PointsTableResult>>
{
    public override void Configure()
    {
        Get("/seasons/{seasonId}/table");
        Group<FootballGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Points Table";
            s.Description = "Returns the points table for a given season, computed from match results";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var seasonId = Route<long>("seasonId");
        var result = await new GetPointsTableQuery(seasonId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
