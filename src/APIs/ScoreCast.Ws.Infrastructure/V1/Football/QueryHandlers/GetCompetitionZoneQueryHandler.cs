using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Application.V1.Football.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetCompetitionZonesQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetCompetitionZonesQuery, ScoreCastResponse<List<CompetitionZoneResult>>>
{
    public async Task<ScoreCastResponse<List<CompetitionZoneResult>>> ExecuteAsync(GetCompetitionZonesQuery query, CancellationToken ct)
    {
        var zones = await DbContext.CompetitionZones
            .AsNoTracking()
            .Where(z => z.Competition.Code == query.CompetitionCode)
            .OrderBy(z => z.SortOrder)
            .Select(z => new CompetitionZoneResult(z.Name, z.Color, z.StartPosition, z.EndPosition))
            .ToListAsync(ct);

        return ScoreCastResponse<List<CompetitionZoneResult>>.Ok(zones);
    }
}
