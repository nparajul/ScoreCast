using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetCompetitionsQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetCompetitionsQuery, ScoreCastResponse<List<CompetitionResult>>>
{
    public async Task<ScoreCastResponse<List<CompetitionResult>>> ExecuteAsync(GetCompetitionsQuery query, CancellationToken ct)
    {
        var competitions = await DbContext.Competitions
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => new CompetitionResult(
                c.Id, c.Name, c.Code, c.LogoUrl, c.Country.Name, c.Country.FlagUrl,
                DbContext.ExternalMappings
                    .Where(m => m.EntityType == EntityType.Competition && m.EntityId == c.Id)
                    .Select(m => m.Source.ToString())
                    .ToList()))
            .ToListAsync(ct);

        return ScoreCastResponse<List<CompetitionResult>>.Ok(competitions);
    }
}
