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
        // Verify requesting user is in the same league
        var requestingUser = await DbContext.UserMasters.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == query.RequestingUserId, ct);
        if (requestingUser is null)
            return ScoreCastResponse<PlayerProfileResult>.Error("Unauthorized");

        var isMember = await DbContext.PredictionLeagueMembers.AsNoTracking()
            .AnyAsync(m => m.PredictionLeagueId == query.PredictionLeagueId && m.UserId == requestingUser.Id, ct);
        if (!isMember)
            return ScoreCastResponse<PlayerProfileResult>.Error("Not a member of this league");

        // Verify target user is also in the league
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

        var scoringRules = await DbContext.PredictionScoringRules.AsNoTracking()
            .Where(r => r.PredictionType == PredictionType.Score && r.StageType == null)
            .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

        var predictions = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.SeasonId == league!.SeasonId && p.UserId == query.TargetUserId && p.Outcome != null)
            .Select(p => new { p.Outcome, p.Match!.GameweekId })
            .ToListAsync(ct);

        var riskBonus = await DbContext.RiskPlays.AsNoTracking()
            .Where(r => r.SeasonId == league!.SeasonId && r.UserId == query.TargetUserId && r.IsResolved == true && !r.IsDeleted)
            .SumAsync(r => r.BonusPoints ?? 0, ct);

        var totalPredictionPoints = predictions.Sum(p => scoringRules.GetValueOrDefault(p.Outcome!.Value, 0));
        var matchweeksPlayed = predictions.Select(p => p.GameweekId).Distinct().Count();

        // Per-gameweek points for average and best
        var gwPoints = predictions
            .GroupBy(p => p.GameweekId)
            .Select(g => g.Sum(p => scoringRules.GetValueOrDefault(p.Outcome!.Value, 0)))
            .ToList();

        var riskByGw = await DbContext.RiskPlays.AsNoTracking()
            .Where(r => r.SeasonId == league!.SeasonId && r.UserId == query.TargetUserId && r.IsResolved == true && !r.IsDeleted)
            .GroupBy(r => r.GameweekId)
            .Select(g => new { GwId = g.Key, Bonus = g.Sum(r => r.BonusPoints ?? 0) })
            .ToDictionaryAsync(x => x.GwId, x => x.Bonus, ct);

        var bestGw = 0;
        decimal avgPts = 0;
        if (gwPoints.Count > 0)
        {
            // Merge risk bonus into per-GW totals
            var allGwIds = predictions.Select(p => p.GameweekId).Distinct().ToList();
            var gwTotals = allGwIds.Select(gwId =>
            {
                var predPts = predictions.Where(p => p.GameweekId == gwId)
                    .Sum(p => scoringRules.GetValueOrDefault(p.Outcome!.Value, 0));
                return predPts + riskByGw.GetValueOrDefault(gwId, 0);
            }).ToList();

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
            predictions.Count(p => p.Outcome == PredictionOutcome.ExactScore),
            predictions.Count(p => p.Outcome == PredictionOutcome.CorrectResult),
            avgPts));
    }
}
