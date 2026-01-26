using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetPlayerProfileQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetPlayerProfileQuery, ScoreCastResponse<PlayerProfileResult>>
{
    public async Task<ScoreCastResponse<PlayerProfileResult>> ExecuteAsync(GetPlayerProfileQuery query, CancellationToken ct)
    {
        var requestingUser = await DbContext.UserMasters.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == query.RequestingUserId, ct);
        if (requestingUser is null)
            return ScoreCastResponse<PlayerProfileResult>.Error("Unauthorized");

        var isMember = await DbContext.PredictionLeagueMembers.AsNoTracking()
            .AnyAsync(m => m.PredictionLeagueId == query.PredictionLeagueId && m.UserId == requestingUser.Id, ct);
        if (!isMember)
            return ScoreCastResponse<PlayerProfileResult>.Error("Not a member of this league");

        var targetMember = await DbContext.PredictionLeagueMembers.AsNoTracking()
            .AnyAsync(m => m.PredictionLeagueId == query.PredictionLeagueId && m.UserId == query.TargetUserId, ct);
        if (!targetMember)
            return ScoreCastResponse<PlayerProfileResult>.Error("Player not found in this league");

        var target = await DbContext.UserMasters.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == query.TargetUserId, ct);
        if (target is null)
            return ScoreCastResponse<PlayerProfileResult>.Error("Player not found");

        var league = await DbContext.PredictionLeagues.AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == query.PredictionLeagueId, ct);

        // Resolve scoped GW ids
        int? startingGwNumber = null;
        if (league!.StartingGameweekId.HasValue)
        {
            startingGwNumber = await DbContext.Gameweeks.AsNoTracking()
                .Where(g => g.Id == league.StartingGameweekId.Value)
                .Select(g => (int?)g.Number)
                .FirstOrDefaultAsync(ct);
        }

        var scopedGwIds = await DbContext.Gameweeks.AsNoTracking()
            .Where(g => g.SeasonId == league.SeasonId
                        && (!startingGwNumber.HasValue || g.Number >= startingGwNumber.Value))
            .Select(g => g.Id)
            .ToHashSetAsync(ct);

        var scoringRules = await DbContext.PredictionScoringRules.AsNoTracking()
            .Where(r => r.PredictionType == PredictionType.Score && r.StageType == null)
            .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

        var predictions = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.SeasonId == league.SeasonId && p.UserId == query.TargetUserId && p.Outcome != null)
            .Select(p => new { p.Outcome, p.Match!.GameweekId })
            .ToListAsync(ct);

        var scopedPredictions = predictions.Where(p => scopedGwIds.Contains(p.GameweekId)).ToList();

        var riskPlays = await DbContext.RiskPlays.AsNoTracking()
            .Where(r => r.SeasonId == league.SeasonId && r.UserId == query.TargetUserId
                        && r.IsResolved == true && !r.IsDeleted && scopedGwIds.Contains(r.GameweekId))
            .Select(r => new { r.GameweekId, r.BonusPoints })
            .ToListAsync(ct);

        var riskBonus = riskPlays.Sum(r => r.BonusPoints ?? 0);
        var totalPredictionPoints = scopedPredictions.Sum(p => scoringRules.GetValueOrDefault(p.Outcome!.Value, 0));
        var matchweeksPlayed = scopedPredictions.Select(p => p.GameweekId).Distinct().Count();

        var bestGw = 0;
        decimal avgPts = 0;
        if (matchweeksPlayed > 0)
        {
            var riskByGw = riskPlays.GroupBy(r => r.GameweekId)
                .ToDictionary(g => g.Key, g => g.Sum(r => r.BonusPoints ?? 0));

            var gwTotals = scopedPredictions.GroupBy(p => p.GameweekId)
                .Select(g => g.Sum(p => scoringRules.GetValueOrDefault(p.Outcome!.Value, 0))
                             + riskByGw.GetValueOrDefault(g.Key, 0))
                .ToList();

            bestGw = gwTotals.Max();
            avgPts = Math.Round((decimal)(totalPredictionPoints + riskBonus) / matchweeksPlayed, 1);
        }

        return ScoreCastResponse<PlayerProfileResult>.Ok(new PlayerProfileResult(
            target.Id,
            target.DisplayName ?? target.UserId,
            target.AvatarUrl,
            target.FavoriteTeam,
            totalPredictionPoints + riskBonus,
            bestGw,
            matchweeksPlayed,
            scopedPredictions.Count(p => p.Outcome == PredictionOutcome.ExactScore),
            scopedPredictions.Count(p => p.Outcome == PredictionOutcome.CorrectResult),
            avgPts));
    }
}
