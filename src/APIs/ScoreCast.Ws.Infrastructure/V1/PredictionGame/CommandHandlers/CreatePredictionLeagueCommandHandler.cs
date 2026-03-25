using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Validation;
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

        if (ProfanityFilter.ContainsProfanity(request.Name))
            return ScoreCastResponse<PredictionLeagueResult>.Error("Please choose an appropriate league name");

        var user = await DbContext.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, ct);

        if (user is null)
            return ScoreCastResponse<PredictionLeagueResult>.Error("User not found");

        var competition = await DbContext.Competitions
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CompetitionId, ct);

        if (competition is null)
            return ScoreCastResponse<PredictionLeagueResult>.Error("Competition not found");

        var season = await DbContext.Seasons
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.CompetitionId == request.CompetitionId && s.IsCurrent, ct);

        if (season is null)
            return ScoreCastResponse<PredictionLeagueResult>.Error("No active season for this competition");

        string inviteCode;
        do
        {
            inviteCode = GenerateInviteCode();
        } while (await DbContext.PredictionLeagues.AnyAsync(l => l.InviteCode == inviteCode, ct));

        // Determine starting gameweek: next GW that hasn't started yet (Upcoming)
        // If current GW is active, start from the next one so players have time to join & predict
        var startingGw = await DbContext.Gameweeks
            .AsNoTracking()
            .Where(g => g.SeasonId == season.Id && g.Status == GameweekStatus.Upcoming)
            .OrderBy(g => g.Number)
            .FirstOrDefaultAsync(ct);

        // Fallback: if no upcoming GW, use the first GW of the season (league created before season starts or at end)
        startingGw ??= await DbContext.Gameweeks
            .AsNoTracking()
            .Where(g => g.SeasonId == season.Id)
            .OrderBy(g => g.Number)
            .FirstOrDefaultAsync(ct);

        var league = new PredictionLeague
        {
            Name = request.Name,
            InviteCode = inviteCode,
            CompetitionId = competition.Id,
            SeasonId = season.Id,
            CreatedByUserId = user.Id,
            StartingGameweekId = startingGw?.Id
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
            new PredictionLeagueResult(league.Id, league.Name, league.InviteCode,
                competition.Id, competition.Name, competition.Code, competition.LogoUrl,
                season.Id, season.Name, 1, user.DisplayName ?? user.UserId));
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
