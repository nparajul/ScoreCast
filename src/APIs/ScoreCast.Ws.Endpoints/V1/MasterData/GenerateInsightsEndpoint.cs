using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Insights.Queries;

namespace ScoreCast.Ws.Endpoints.V1.MasterData;

public sealed class GenerateInsightsEndpoint : EndpointWithoutRequest<ScoreCastResponse>
{
    public override void Configure()
    {
        Post("/generate-insights");
        Group<MasterDataGroup>();
        Summary(s => s.Description = "Pre-generate AI insights for current and next gameweek");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var comps = await new GetCompetitionsQuery().ExecuteAsync(ct);
        if (comps is not { Success: true, Data: not null })
        {
            await Send.OkAsync(ScoreCastResponse.Ok("No competitions found"), ct);
            return;
        }

        var generated = 0;
        foreach (var comp in comps.Data)
        {
            var seasons = await new GetSeasonsQuery(comp.Code).ExecuteAsync(ct);
            var current = seasons.Data?.FirstOrDefault(s => s.IsCurrent);
            if (current is null) continue;

            var gw = await new GetGameweekMatchesQuery(current.Id).ExecuteAsync(ct);
            if (gw is not { Success: true, Data: not null }) continue;

            var result = await new GetMatchInsightsQuery(current.Id, gw.Data.CurrentGameweek).ExecuteAsync(ct);
            if (result is { Success: true, Data.Count: > 0 }) generated += result.Data.Count;

            if (gw.Data.CurrentGameweek < gw.Data.TotalGameweeks)
            {
                var next = await new GetMatchInsightsQuery(current.Id, gw.Data.CurrentGameweek + 1).ExecuteAsync(ct);
                if (next is { Success: true, Data.Count: > 0 }) generated += next.Data.Count;
            }
        }

        await Send.OkAsync(ScoreCastResponse.Ok($"Generated insights for {generated} matches"), ct);
    }
}
