using ScoreCast.Models.V1.Requests.MasterData;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.MasterData.Commands;

namespace ScoreCast.Ws.Endpoints.V1.MasterData;

public sealed class EnhanceLiveMatchesEndpoint : Endpoint<EnhanceLiveMatchesRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/enhance-live");
        Group<MasterDataGroup>();
    }

    public override async Task HandleAsync(EnhanceLiveMatchesRequest req, CancellationToken ct)
    {
        var result = await new EnhanceLiveMatchesCommand(req).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
