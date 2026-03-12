using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.UserManagement;
using ScoreCast.Ws.Application.Interfaces;
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
            .FirstOrDefaultAsync(u => u.KeycloakUserId == request.KeycloakUserId, ct);

        if (existingUser is not null)
        {
            existingUser.Email = request.Email;
            existingUser.DisplayName = request.DisplayName ?? existingUser.DisplayName;

            await UnitOfWork.SaveChangesAsync(nameof(SyncUserCommand), ct);

            return ScoreCastResponse<SyncUserResult>.Ok(
                new SyncUserResult(existingUser.Id, existingUser.UserId, existingUser.Email, existingUser.DisplayName, false));
        }

        var newUser = new UserMaster
        {
            KeycloakUserId = request.KeycloakUserId,
            UserId = request.UserId ?? request.KeycloakUserId,
            Email = request.Email,
            DisplayName = request.DisplayName,
        };

        DbContext.UserMasters.Add(newUser);
        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(SyncUserCommand), ct);

        return ScoreCastResponse<SyncUserResult>.Ok(
            new SyncUserResult(newUser.Id, newUser.UserId, newUser.Email, newUser.DisplayName, true));
    }
}
