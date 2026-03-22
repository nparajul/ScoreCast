using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class ReorderUserSeasonsEndpoint : Endpoint<ReorderUserSeasonsRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Put("/user-seasons/reorder");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(ReorderUserSeasonsRequest request, CancellationToken ct)
    {
        var result = await new ReorderUserSeasonsCommand(request).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
