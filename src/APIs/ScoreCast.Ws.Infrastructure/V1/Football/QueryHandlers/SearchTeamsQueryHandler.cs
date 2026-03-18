using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record SearchTeamsQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<SearchTeamsQuery, ScoreCastResponse<TeamSearchResult>>
{
    public async Task<ScoreCastResponse<TeamSearchResult>> ExecuteAsync(SearchTeamsQuery query, CancellationToken ct)
    {
        var teamsQuery = DbContext.Teams
            .AsNoTracking()
            .Where(t => t.IsActive);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = $"%{query.SearchTerm}%";
            teamsQuery = teamsQuery.Where(t => EF.Functions.ILike(t.Name, term) || (t.ShortName != null && EF.Functions.ILike(t.ShortName, term)));
        }

        var teams = await teamsQuery
            .OrderBy(t => t.Name)
            .Skip(query.Skip)
            .Take(query.Take + 1)
            .Select(t => new TeamResult(t.Id, t.Name, t.ShortName, t.LogoUrl))
            .ToListAsync(ct);

        var hasMore = teams.Count > query.Take;
        if (hasMore)
            teams.RemoveAt(teams.Count - 1);

        return ScoreCastResponse<TeamSearchResult>.Ok(new TeamSearchResult(teams, hasMore));
    }
}
