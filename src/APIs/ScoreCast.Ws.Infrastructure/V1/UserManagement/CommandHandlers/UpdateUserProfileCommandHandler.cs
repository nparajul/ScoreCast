using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.UserManagement.Commands;

namespace ScoreCast.Ws.Infrastructure.V1.UserManagement.CommandHandlers;

internal sealed record UpdateUserProfileCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<UpdateUserProfileCommand, ScoreCastResponse<UserProfileResult>>
{
    public async Task<ScoreCastResponse<UserProfileResult>> ExecuteAsync(UpdateUserProfileCommand command, CancellationToken ct)
    {
        var user = await DbContext.UserMasters
            .FirstOrDefaultAsync(u => u.KeycloakUserId == command.KeycloakUserId, ct);

        if (user is null)
            return ScoreCastResponse<UserProfileResult>.NotFound("User profile not found");

        var req = command.Request;
        user.DisplayName = req.DisplayName ?? user.DisplayName;
        user.AvatarUrl = req.AvatarUrl ?? user.AvatarUrl;
        user.FavoriteTeam = req.FavoriteTeam ?? user.FavoriteTeam;

        await UnitOfWork.SaveChangesAsync(req.AppName ?? nameof(UpdateUserProfileCommand), ct);

        return ScoreCastResponse<UserProfileResult>.Ok(
            new UserProfileResult(
                user.Id, user.UserId, user.Email, user.DisplayName,
                user.AvatarUrl, user.FavoriteTeam, user.TotalPoints,
                user.BestGameweek, 0, user.IsActive,
                user.CreatedDate));
    }
}
