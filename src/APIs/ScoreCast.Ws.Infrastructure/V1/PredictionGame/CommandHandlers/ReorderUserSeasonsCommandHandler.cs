using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;
using ScoreCast.Ws.Infrastructure.V1.Shared;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.CommandHandlers;

internal sealed record ReorderUserSeasonsCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<ReorderUserSeasonsCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(ReorderUserSeasonsCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var user = await DbContext.UserMasters.FirstOrDefaultAsync(u => u.UserId == req.UserId, ct);
        if (user is null) return ScoreCastResponse.Error("User not found");

        var userSeasons = await DbContext.UserSeasons
            .Where(us => us.UserId == user.Id)
            .ToListAsync(ct);

        for (var i = 0; i < req.SeasonIds.Count; i++)
        {
            var us = userSeasons.FirstOrDefault(s => s.SeasonId == req.SeasonIds[i]);
            if (us is not null) us.DisplayOrder = i;
        }

        await UnitOfWork.SaveChangesAsync(req.AppName ?? nameof(ReorderUserSeasonsCommand), ct);
        return ScoreCastResponse.Ok("Order saved");
    }
}
