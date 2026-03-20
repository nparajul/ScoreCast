using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Types;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.UserManagement.Commands;
using ScoreCast.Ws.Domain.V1.Entities.UserManagement;

namespace ScoreCast.Ws.Infrastructure.V1.UserManagement.CommandHandlers;

internal sealed record SyncUserCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<SyncUserCommand, ScoreCastResponse<SyncUserResult>>
{
    public async Task<ScoreCastResponse<SyncUserResult>> ExecuteAsync(SyncUserCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var existingUser = await DbContext.UserMasters
            .FirstOrDefaultAsync(u => u.KeycloakUserId == request.KeycloakUserId
                                      || u.Email == request.Email, ct);

        if (existingUser is not null)
        {
            existingUser.KeycloakUserId = request.KeycloakUserId ?? existingUser.KeycloakUserId;
            existingUser.Email = request.Email;
            existingUser.DisplayName = request.DisplayName ?? existingUser.DisplayName;
            existingUser.LastLoginDate = ScoreCastDateTime.Now;

            await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(SyncUserCommand), ct);

            return ScoreCastResponse<SyncUserResult>.Ok(
                new SyncUserResult(existingUser.Id, existingUser.UserId, existingUser.Email, existingUser.DisplayName, false));
        }

        var newUser = new UserMaster
        {
            KeycloakUserId = request.KeycloakUserId!,
            UserId = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        DbContext.UserMasters.Add(newUser);

        var defaultRole = await DbContext.RoleMasters
            .FirstOrDefaultAsync(r => r.Name == RoleNames.User, ct);

        if (defaultRole is not null)
            DbContext.UserRoles.Add(new UserRole {  User = newUser, Role = defaultRole });

        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(SyncUserCommand), ct);

        return ScoreCastResponse<SyncUserResult>.Ok(
            new SyncUserResult(newUser.Id, newUser.UserId, newUser.Email, newUser.DisplayName, true));
    }
}
