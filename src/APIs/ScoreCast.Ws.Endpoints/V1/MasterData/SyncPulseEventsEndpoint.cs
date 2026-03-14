using ScoreCast.Models.V1.Requests.MasterData;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.MasterData;
using ScoreCast.Ws.Application.V1.MasterData.Commands;

namespace ScoreCast.Ws.Endpoints.V1.MasterData;

public sealed class SyncPulseEventsEndpoint : Endpoint<SyncPulseEventsRequest, ScoreCastResponse<SyncPulseEventsResult>>
{
    public override void Configure()
    {
        Post("/sync/pulse-events");
        Group<MasterDataGroup>();
    }

    public override async Task HandleAsync(SyncPulseEventsRequest req, CancellationToken ct)
    {
        var result = await new SyncPulseEventsCommand(req).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
