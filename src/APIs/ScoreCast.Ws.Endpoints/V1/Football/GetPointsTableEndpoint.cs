using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetPointsTableEndpoint : Endpoint<GetPointsTableRequest, ScoreCastResponse<PointsTableResult>>
{
    public override void Configure()
    {
        Get("/seasons/{SeasonId}/table");
        Group<FootballGroup>();
        Summary(s =>
        {
            s.Summary = "Get Points Table";
            s.Description = "Returns the points table for a given season, computed from match results";
        });
    }

    public override async Task HandleAsync(GetPointsTableRequest req, CancellationToken ct)
    {
        var result = await new GetPointsTableQuery(req.SeasonId).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
