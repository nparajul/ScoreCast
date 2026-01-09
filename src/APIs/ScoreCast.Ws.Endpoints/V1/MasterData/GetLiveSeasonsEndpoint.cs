using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.MasterData.Queries;

namespace ScoreCast.Ws.Endpoints.V1.MasterData;

public sealed class GetLiveSeasonsEndpoint : EndpointWithoutRequest<ScoreCastResponse<List<long>>>
{
    public override void Configure()
    {
        Get("/enhance-live/seasons");
        Group<MasterDataGroup>();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await new GetLiveSeasonsQuery().ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
