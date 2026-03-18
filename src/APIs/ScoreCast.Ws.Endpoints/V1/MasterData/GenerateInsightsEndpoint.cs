using ScoreCast.Models.V1.Requests;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.Insights.Commands;

namespace ScoreCast.Ws.Endpoints.V1.MasterData;

public sealed class GenerateInsightsEndpoint : Endpoint<ScoreCastRequest, ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/generate-insights");
        Group<MasterDataGroup>();
        Summary(s =>
        {
            s.Summary = "Generate Insights";
            s.Description = "Pre-generates AI insights for current and next gameweek";
        });
    }

    public override async Task HandleAsync(ScoreCastRequest req, CancellationToken ct)
    {
        var result = await new GenerateInsightsCommand(req).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
