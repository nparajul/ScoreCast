using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.MasterData.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.MasterData.QueryHandlers;

internal sealed record GetLiveSeasonsQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetLiveSeasonsQuery, ScoreCastResponse<List<long>>>
{
    public async Task<ScoreCastResponse<List<long>>> ExecuteAsync(GetLiveSeasonsQuery query, CancellationToken ct)
    {
        var seasonIds = await DbContext.Seasons
            .Where(s => s.IsCurrent)
            .Select(s => s.Id)
            .ToListAsync(ct);

        return ScoreCastResponse<List<long>>.Ok(seasonIds);
    }
}
