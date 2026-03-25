using System.Text.RegularExpressions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.UserManagement.Commands;

namespace ScoreCast.Ws.Infrastructure.V1.UserManagement.CommandHandlers;

internal sealed partial record SetUsernameCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<SetUsernameCommand, ScoreCastResponse<UserProfileResult>>
{
    public async Task<ScoreCastResponse<UserProfileResult>> ExecuteAsync(SetUsernameCommand command, CancellationToken ct)
    {
        var username = command.Request.Username.Trim().ToLowerInvariant();

        if (username.Length < 3 || username.Length > 20)
            return ScoreCastResponse<UserProfileResult>.Error("Username must be 3–20 characters");

        if (!UsernameRegex().IsMatch(username))
            return ScoreCastResponse<UserProfileResult>.Error("Username can only contain letters, numbers, and underscores");

        var user = await DbContext.UserMasters
            .FirstOrDefaultAsync(u => u.FirebaseUid == command.FirebaseUid, ct);

        if (user is null)
            return ScoreCastResponse<UserProfileResult>.NotFound("User not found");

        var taken = await DbContext.UserMasters
            .AnyAsync(u => u.UserId == username && u.Id != user.Id, ct);

        if (taken)
            return ScoreCastResponse<UserProfileResult>.Error("Username is already taken");

        user.UserId = username;
        await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(SetUsernameCommand), ct);

        return ScoreCastResponse<UserProfileResult>.Ok(
            new UserProfileResult(
                user.Id, user.UserId, user.Email, user.DisplayName,
                user.AvatarUrl, user.FavoriteTeam, user.TotalPoints,
                user.BestGameweek, 0, user.IsActive,
                user.CreatedDate, user.HasCompletedOnboarding));
    }

    [GeneratedRegex("^[a-z0-9_]+$")]
    private static partial Regex UsernameRegex();
}
