using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Insights.Queries;

namespace ScoreCast.Ws.Endpoints.V1.Insights;

public sealed class GetMatchInsightsEndpoint
    : Endpoint<GetMatchInsightsRequest, ScoreCastResponse<List<MatchInsightResult>>>
{
    public override void Configure()
    {
        Get("upcoming");
        Group<InsightsGroup>();
        Summary(s => s.Description = "Get AI-powered insights for upcoming matches");
    }

    public override async Task HandleAsync(GetMatchInsightsRequest req, CancellationToken ct)
    {
        var result = await new GetMatchInsightsQuery(req.SeasonId, req.GameweekNumber).ExecuteAsync(ct);
        await Send.OkAsync(result, ct);
    }
}
