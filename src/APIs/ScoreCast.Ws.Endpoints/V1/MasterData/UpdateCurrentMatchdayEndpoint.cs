using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.Insights.Commands;

namespace ScoreCast.Ws.Endpoints.V1.MasterData;

public sealed class UpdateCurrentMatchdayEndpoint : EndpointWithoutRequest<ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/update-matchday");
        Group<MasterDataGroup>();
        Summary(s =>
        {
            s.Summary = "Update Current Matchday";
            s.Description = "Updates current_matchday for all active seasons based on match completion status";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await new UpdateCurrentMatchdayCommand().ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
