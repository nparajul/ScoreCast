using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.CommandHandlers;

internal sealed record EnrollUserSeasonCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<EnrollUserSeasonCommand, ScoreCastResponse<UserSeasonResult>>
{
    public async Task<ScoreCastResponse<UserSeasonResult>> ExecuteAsync(EnrollUserSeasonCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var user = await DbContext.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, ct);

        if (user is null)
            return ScoreCastResponse<UserSeasonResult>.Error("User not found");

        var season = await DbContext.Seasons
            .AsNoTracking()
            .Include(s => s.Competition)
            .FirstOrDefaultAsync(s => s.Id == request.SeasonId, ct);

        if (season is null)
            return ScoreCastResponse<UserSeasonResult>.Error("Season not found");

        var exists = await DbContext.UserSeasons
            .AnyAsync(us => us.UserId == user.Id && us.SeasonId == season.Id, ct);

        if (exists)
            return ScoreCastResponse<UserSeasonResult>.Error("Already enrolled in this season");

        var userSeason = new UserSeason { UserId = user.Id, SeasonId = season.Id };
        DbContext.UserSeasons.Add(userSeason);
        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(EnrollUserSeasonCommand), ct);

        return ScoreCastResponse<UserSeasonResult>.Ok(new UserSeasonResult(
            userSeason.Id, season.Id, season.Name,
            season.CompetitionId, season.Competition.Name, season.Competition.Code, season.Competition.LogoUrl));
    }
}
