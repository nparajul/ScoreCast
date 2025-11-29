using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetSeasonsQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetSeasonsQuery, ScoreCastResponse<List<SeasonResult>>>
{
    public async Task<ScoreCastResponse<List<SeasonResult>>> ExecuteAsync(GetSeasonsQuery query, CancellationToken ct)
    {
        var seasons = await DbContext.Seasons
            .AsNoTracking()
            .Where(s => s.Competition.Code == query.CompetitionCode)
            .OrderByDescending(s => s.StartDate)
            .Select(s => new SeasonResult(s.Id, s.Name, s.StartDate, s.EndDate, s.IsCurrent))
            .ToListAsync(ct);

        return ScoreCastResponse<List<SeasonResult>>.Ok(seasons);
    }
}
