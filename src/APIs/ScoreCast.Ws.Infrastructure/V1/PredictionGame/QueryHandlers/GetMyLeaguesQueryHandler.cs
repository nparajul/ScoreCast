using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetMyLeaguesQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetMyLeaguesQuery, ScoreCastResponse<List<PredictionLeagueResult>>>
{
    public async Task<ScoreCastResponse<List<PredictionLeagueResult>>> ExecuteAsync(GetMyLeaguesQuery query, CancellationToken ct)
    {
        var user = await DbContext.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == query.Request.UserId, ct);

        if (user is null)
            return ScoreCastResponse<List<PredictionLeagueResult>>.Error("User not found");

        var leagues = await DbContext.PredictionLeagueMembers
            .AsNoTracking()
            .Where(m => m.UserId == user.Id)
            .Select(m => new PredictionLeagueResult(
                m.PredictionLeague.Id,
                m.PredictionLeague.Name,
                m.PredictionLeague.InviteCode,
                m.PredictionLeague.CompetitionId,
                m.PredictionLeague.Competition.Name,
                m.PredictionLeague.Competition.Code,
                m.PredictionLeague.Competition.LogoUrl,
                m.PredictionLeague.SeasonId,
                m.PredictionLeague.Season.Name,
                m.PredictionLeague.Members.Count,
                m.PredictionLeague.CreatedByUser.DisplayName ?? m.PredictionLeague.CreatedByUser.UserId))
            .ToListAsync(ct);

        return ScoreCastResponse<List<PredictionLeagueResult>>.Ok(leagues);
    }
}
