using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Insights.Commands;
using ScoreCast.Ws.Application.V1.Insights.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Insights.CommandHandlers;

internal sealed record GenerateInsightsCommandHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GenerateInsightsCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(GenerateInsightsCommand command, CancellationToken ct)
    {
        var comps = await new GetCompetitionsQuery().ExecuteAsync(ct);
        if (comps is not { Success: true, Data: not null })
            return ScoreCastResponse.Ok("No competitions found");

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

        return ScoreCastResponse.Ok($"Generated insights for {generated} matches");
    }
}
