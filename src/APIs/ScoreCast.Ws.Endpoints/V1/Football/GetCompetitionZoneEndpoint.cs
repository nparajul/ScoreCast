using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetCompetitionZonesEndpoint : Endpoint<GetCompetitionZonesRequest, ScoreCastResponse<List<CompetitionZoneResult>>>
{
    public override void Configure()
    {
        Get("/competitions/{CompetitionCode}/zones");
        Group<FootballGroup>();
        Summary(s =>
        {
            s.Summary = "Get Competition Zones";
            s.Description = "Returns the league table zone config (promotion, qualification, relegation, etc.) for a competition";
        });
    }

    public override async Task HandleAsync(GetCompetitionZonesRequest req, CancellationToken ct)
    {
        var result = await new GetCompetitionZonesQuery(req.CompetitionCode).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
