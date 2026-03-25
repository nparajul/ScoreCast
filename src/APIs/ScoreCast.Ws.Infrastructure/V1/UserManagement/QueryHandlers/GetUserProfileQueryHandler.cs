using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.UserManagement.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.UserManagement.QueryHandlers;

internal sealed record GetUserProfileQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetUserProfileQuery, ScoreCastResponse<UserProfileResult>>
{
    public async Task<ScoreCastResponse<UserProfileResult>> ExecuteAsync(GetUserProfileQuery query, CancellationToken ct)
    {
        var user = await DbContext.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.FirebaseUid == query.FirebaseUid, ct);

        if (user is null)
            return ScoreCastResponse<UserProfileResult>.NotFound("User profile not found");

        var completedGws = await DbContext.UserSeasons
            .Where(us => us.UserId == user.Id)
            .SelectMany(us => us.Season.Gameweeks)
            .Where(gw => gw.Matches.Any(m => m.Status == MatchStatus.Finished))
            .CountAsync(ct);

        return ScoreCastResponse<UserProfileResult>.Ok(
            new UserProfileResult(
                user.Id, user.UserId, user.Email, user.DisplayName,
                user.AvatarUrl, user.FavoriteTeam, user.TotalPoints,
                user.BestGameweek, completedGws, user.IsActive,
                user.CreatedDate, user.HasCompletedOnboarding));
    }
}
