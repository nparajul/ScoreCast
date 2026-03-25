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
            .FirstOrDefaultAsync(u => u.FirebaseUid == request.FirebaseUid
                                      || u.Email == request.Email, ct);

        if (existingUser is not null)
        {
            existingUser.FirebaseUid = request.FirebaseUid ?? existingUser.FirebaseUid;
            existingUser.Email = request.Email;
            existingUser.DisplayName = request.DisplayName ?? existingUser.DisplayName;
            existingUser.LastLoginDate = ScoreCastDateTime.Now;

            await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(SyncUserCommand), ct);

            return ScoreCastResponse<SyncUserResult>.Ok(
                new SyncUserResult(existingUser.Id, existingUser.UserId, existingUser.Email, existingUser.DisplayName, false));
        }

        var userId = await GenerateUniqueUserId(request.Email, ct);

        var newUser = new UserMaster
        {
            FirebaseUid = request.FirebaseUid!,
            UserId = userId,
            Email = request.Email,
            DisplayName = request.IsGoogleSignIn ? userId : request.DisplayName ?? userId,
            CreatedBy = userId,
            ModifiedBy = userId,
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

    private async Task<string> GenerateUniqueUserId(string email, CancellationToken ct)
    {
        var prefix = email.Split('@')[0]
            .ToLowerInvariant()
            .Replace(".", "")
            .Replace("+", "");

        // Keep only valid chars (letters, digits, underscores), max 15 chars to leave room for suffix
        prefix = new string(prefix.Where(c => char.IsLetterOrDigit(c) || c == '_').Take(15).ToArray());
        if (prefix.Length < 3) prefix = "user";

        var candidate = prefix;
        if (!await DbContext.UserMasters.AnyAsync(u => u.UserId == candidate, ct))
            return candidate;

        // Append random digits until unique
        var rng = new Random();
        for (var i = 0; i < 20; i++)
        {
            candidate = $"{prefix}{rng.Next(100, 9999)}";
            if (!await DbContext.UserMasters.AnyAsync(u => u.UserId == candidate, ct))
                return candidate;
        }

        return $"{prefix}{Guid.NewGuid():N}"[..20];
    }
}
