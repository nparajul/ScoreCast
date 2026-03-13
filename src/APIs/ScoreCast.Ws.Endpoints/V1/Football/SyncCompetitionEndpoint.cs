using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.Football.Commands;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class SyncCompetitionEndpoint : Endpoint<SyncCompetitionRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/sync/competition/{competitionCode}");
        Group<FootballGroup>();
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Sync Competition";
            s.Description = "Syncs a competition and its seasons from football-data.org";
        });
    }

    public override async Task HandleAsync(SyncCompetitionRequest req, CancellationToken ct)
    {
        var result = await new SyncCompetitionCommand(req).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
