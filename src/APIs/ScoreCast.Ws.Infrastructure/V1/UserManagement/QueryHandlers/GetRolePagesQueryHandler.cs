using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Application.V1.UserManagement.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.UserManagement.QueryHandlers;

internal sealed record GetRolePagesQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetRolePagesQuery, ScoreCastResponse<List<PageResult>>>
{
    public async Task<ScoreCastResponse<List<PageResult>>> ExecuteAsync(GetRolePagesQuery query, CancellationToken ct)
    {
        var pages = await DbContext.RolePages
            .AsNoTracking()
            .Where(rp => rp.RoleId == query.RoleId)
            .Select(rp => new PageResult(rp.Page.Id, rp.Page.PageCode, rp.Page.PageName, rp.Page.PageUrl, rp.Page.ParentPageId))
            .ToListAsync(ct);

        return ScoreCastResponse<List<PageResult>>.Ok(pages);
    }
}
