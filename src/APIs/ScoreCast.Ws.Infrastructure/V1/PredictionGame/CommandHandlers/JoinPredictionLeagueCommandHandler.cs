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

internal sealed record JoinPredictionLeagueCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<JoinPredictionLeagueCommand, ScoreCastResponse<PredictionLeagueResult>>
{
    public async Task<ScoreCastResponse<PredictionLeagueResult>> ExecuteAsync(JoinPredictionLeagueCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var user = await DbContext.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, ct);

        if (user is null)
            return ScoreCastResponse<PredictionLeagueResult>.Error("User not found");

        var league = await DbContext.PredictionLeagues
            .Include(l => l.Members)
            .Include(l => l.Season)
            .Include(l => l.CreatedByUser)
            .FirstOrDefaultAsync(l => l.InviteCode == request.InviteCode, ct);

        if (league is null)
            return ScoreCastResponse<PredictionLeagueResult>.Error("Invalid invite code");

        if (league.Members.Any(m => m.UserId == user.Id))
            return ScoreCastResponse<PredictionLeagueResult>.Error("Already a member of this league");

        var member = new PredictionLeagueMember
        {
            PredictionLeague = league,
            UserId = user.Id,
            Role = PredictionLeagueMemberRole.Member
        };

        DbContext.PredictionLeagueMembers.Add(member);

        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(JoinPredictionLeagueCommand), ct);

        var memberCount = await DbContext.PredictionLeagueMembers
            .CountAsync(m => m.PredictionLeagueId == league.Id, ct);

        return ScoreCastResponse<PredictionLeagueResult>.Ok(
            new PredictionLeagueResult(league.Id, league.Name, league.InviteCode, league.SeasonId,
                league.Season.Name, memberCount,
                league.CreatedByUser.DisplayName ?? league.CreatedByUser.UserId));
    }
}
