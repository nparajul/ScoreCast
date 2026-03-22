using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetUserSeasonsQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetUserSeasonsQuery, ScoreCastResponse<List<UserSeasonResult>>>
{
    public async Task<ScoreCastResponse<List<UserSeasonResult>>> ExecuteAsync(GetUserSeasonsQuery query, CancellationToken ct)
    {
        var request = query.Request;

        var user = await DbContext.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, ct);

        if (user is null)
            return ScoreCastResponse<List<UserSeasonResult>>.Error("User not found");

        var results = await DbContext.UserSeasons
            .AsNoTracking()
            .Where(us => us.UserId == user.Id)
            .Include(us => us.Season).ThenInclude(s => s.Competition)
            .OrderBy(us => us.DisplayOrder)
            .Select(us => new UserSeasonResult(
                us.Id, us.SeasonId, us.Season.Name,
                us.Season.CompetitionId, us.Season.Competition.Name,
                us.Season.Competition.Code, us.Season.Competition.LogoUrl,
                us.DisplayOrder))
            .ToListAsync(ct);

        return ScoreCastResponse<List<UserSeasonResult>>.Ok(results);
    }
}
