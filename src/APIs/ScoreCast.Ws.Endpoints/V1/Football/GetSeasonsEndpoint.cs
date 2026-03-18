using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetSeasonsEndpoint : Endpoint<GetSeasonsRequest, ScoreCastResponse<List<SeasonResult>>>
{
    public override void Configure()
    {
        Get("/competitions/{CompetitionCode}/seasons");
        Group<FootballGroup>();
        Summary(s =>
        {
            s.Summary = "Get Seasons";
            s.Description = "Returns all seasons for a competition";
        });
    }

    public override async Task HandleAsync(GetSeasonsRequest req, CancellationToken ct)
    {
        var result = await new GetSeasonsQuery(req.CompetitionCode).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
