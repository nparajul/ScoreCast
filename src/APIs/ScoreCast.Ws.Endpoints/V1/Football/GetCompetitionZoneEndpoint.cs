using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class GetCompetitionZonesEndpoint : EndpointWithoutRequest<ScoreCastResponse<List<CompetitionZoneResult>>>
{
    public override void Configure()
    {
        Get("/competitions/{competitionCode}/zones");
        Group<FootballGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get Competition Zones";
            s.Description = "Returns the league table zone config (promotion, qualification, relegation, etc.) for a competition";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var code = Route<string>("competitionCode")!;
        var result = await new GetCompetitionZonesQuery(code).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
