using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.Football.Commands;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class SyncTeamsEndpoint : Endpoint<SyncCompetitionRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/sync/teams");
        Group<FootballGroup>();
        Summary(s =>
        {
            s.Summary = "Sync Teams";
            s.Description = "Syncs all teams for a competition from football-data.org";
        });
    }

    public override async Task HandleAsync(SyncCompetitionRequest req, CancellationToken ct)
    {
        var result = await new SyncTeamsCommand(req).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
