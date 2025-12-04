using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Commands;

namespace ScoreCast.Ws.Endpoints.V1.Football;

public sealed class SyncPulseEventsEndpoint : Endpoint<SyncPulseEventsRequest, ScoreCastResponse<SyncPulseEventsResult>>
{
    public override void Configure()
    {
        Post("/sync/pulse-events");
        Group<FootballGroup>();
    }

    public override async Task HandleAsync(SyncPulseEventsRequest req, CancellationToken ct)
    {
        var result = await new SyncPulseEventsCommand(req).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
