using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;

namespace ScoreCast.Ws.Endpoints.V1.PredictionGame;

public sealed class EnrollUserSeasonEndpoint : Endpoint<EnrollUserSeasonRequest, ScoreCastResponse<UserSeasonResult>>
{
    public override void Configure()
    {
        Post("/user-seasons");
        Group<PredictionGroup>();
    }

    public override async Task HandleAsync(EnrollUserSeasonRequest request, CancellationToken ct)
    {
        var result = await new EnrollUserSeasonCommand(request).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
