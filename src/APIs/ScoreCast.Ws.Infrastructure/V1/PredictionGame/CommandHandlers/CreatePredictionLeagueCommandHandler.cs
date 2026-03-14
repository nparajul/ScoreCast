using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.CommandHandlers;

internal sealed record CreatePredictionLeagueCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<CreatePredictionLeagueCommand, ScoreCastResponse<PredictionLeagueResult>>
{
    public async Task<ScoreCastResponse<PredictionLeagueResult>> ExecuteAsync(CreatePredictionLeagueCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var user = await DbContext.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, ct);

        if (user is null)
            return ScoreCastResponse<PredictionLeagueResult>.Error("User not found");

        var season = await DbContext.Seasons
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == request.SeasonId, ct);

        if (season is null)
            return ScoreCastResponse<PredictionLeagueResult>.Error("Season not found");

        string inviteCode;
        do
        {
            inviteCode = GenerateInviteCode();
        } while (await DbContext.PredictionLeagues.AnyAsync(l => l.InviteCode == inviteCode, ct));

        var league = new PredictionLeague
        {
            Name = request.Name,
            InviteCode = inviteCode,
            SeasonId = request.SeasonId,
            CreatedByUserId = user.Id
        };

        DbContext.PredictionLeagues.Add(league);

        var member = new PredictionLeagueMember
        {
            PredictionLeague = league,
            UserId = user.Id,
            Role = PredictionLeagueMemberRole.Owner
        };

        DbContext.PredictionLeagueMembers.Add(member);

        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(CreatePredictionLeagueCommand), ct);

        return ScoreCastResponse<PredictionLeagueResult>.Ok(
            new PredictionLeagueResult(league.Id, league.Name, league.InviteCode, league.SeasonId,
                season.Name, 1, user.DisplayName ?? user.UserId));
    }

    private static string GenerateInviteCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return string.Create(6, chars, (span, state) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = state[Random.Shared.Next(state.Length)];
        });
    }
}
