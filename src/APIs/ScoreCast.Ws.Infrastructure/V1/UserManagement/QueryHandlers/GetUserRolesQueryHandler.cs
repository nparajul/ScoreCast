using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.UserManagement.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.UserManagement.QueryHandlers;

internal sealed record GetUserRolesQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetUserRolesQuery, ScoreCastResponse<List<RoleResult>>>
{
    public async Task<ScoreCastResponse<List<RoleResult>>> ExecuteAsync(GetUserRolesQuery query, CancellationToken ct)
    {
        var roles = await DbContext.UserRoles
            .AsNoTracking()
            .Where(ur => ur.User.KeycloakUserId == query.KeycloakUserId && ur.Role.IsActive)
            .OrderBy(ur => ur.Role.SortOrder)
            .Select(ur => new RoleResult(ur.Role.Id, ur.Role.Name))
            .ToListAsync(ct);

        return ScoreCastResponse<List<RoleResult>>.Ok(roles);
    }
}
