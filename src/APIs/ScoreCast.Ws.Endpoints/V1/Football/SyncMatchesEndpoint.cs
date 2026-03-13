using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.Football.Commands;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class SyncMatchesEndpoint : Endpoint<SyncCompetitionRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/sync/matches");
        Group<FootballGroup>();
        Summary(s =>
        {
            s.Summary = "Sync Matches";
            s.Description = "Syncs all matches for all accessible seasons of a competition from football-data.org";
        });
    }

    public override async Task HandleAsync(SyncCompetitionRequest req, CancellationToken ct)
    {
        var result = await new SyncMatchesCommand(req).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
