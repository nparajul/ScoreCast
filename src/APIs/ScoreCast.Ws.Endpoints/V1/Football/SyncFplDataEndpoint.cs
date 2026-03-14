using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.Football.Commands;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class SyncFplDataEndpoint : Endpoint<SyncCompetitionRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/sync/fpl");
        Group<FootballGroup>();
        Summary(s =>
        {
            s.Summary = "Sync FPL Data";
            s.Description = "Syncs match events (goals, assists, cards) from the Fantasy Premier League API";
        });
    }

    public override async Task HandleAsync(SyncCompetitionRequest req, CancellationToken ct)
    {
        var result = await new SyncFplDataCommand(req).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
