using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetLeagueStandingsQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetLeagueStandingsQuery, ScoreCastResponse<LeagueStandingsResult>>
{
    public async Task<ScoreCastResponse<LeagueStandingsResult>> ExecuteAsync(GetLeagueStandingsQuery query, CancellationToken ct)
    {
        var league = await DbContext.PredictionLeagues
            .AsNoTracking()
            .Include(l => l.Competition)
            .FirstOrDefaultAsync(l => l.Id == query.PredictionLeagueId, ct);

        if (league is null)
            return ScoreCastResponse<LeagueStandingsResult>.Error("League not found");

        // Resolve starting gameweek number for scoping
        int? startingGwNumber = null;
        long? startingGwId = league.StartingGameweekId;
        if (startingGwId.HasValue)
        {
            startingGwNumber = await DbContext.Gameweeks.AsNoTracking()
                .Where(g => g.Id == startingGwId.Value)
                .Select(g => (int?)g.Number)
                .FirstOrDefaultAsync(ct);
        }

        // Get all GW ids in scope
        var scopedGwIds = await DbContext.Gameweeks.AsNoTracking()
            .Where(g => g.SeasonId == league.SeasonId
                        && (!startingGwNumber.HasValue || g.Number >= startingGwNumber.Value))
            .Select(g => g.Id)
            .ToListAsync(ct);

        var scopedGwIdSet = scopedGwIds.ToHashSet();

        var scoringRules = await DbContext.PredictionScoringRules
            .AsNoTracking()
            .Where(r => r.PredictionType == PredictionType.Score && r.StageType == null)
            .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

        var members = await DbContext.PredictionLeagueMembers
            .AsNoTracking()
            .Where(m => m.PredictionLeagueId == query.PredictionLeagueId)
            .Select(m => new { m.UserId, m.User.DisplayName, m.User.AvatarUrl, UserIdString = m.User.UserId })
            .ToListAsync(ct);

        var memberUserIds = members.Select(m => m.UserId).ToList();

        var predictions = await DbContext.Predictions
            .AsNoTracking()
            .Where(p => p.SeasonId == league.SeasonId
                        && memberUserIds.Contains(p.UserId)
                        && p.Outcome != null)
            .Select(p => new { p.UserId, p.Outcome, GameweekId = p.Match!.GameweekId })
            .ToListAsync(ct);

        // Filter to scoped GWs in memory
        var scopedPredictions = predictions.Where(p => scopedGwIdSet.Contains(p.GameweekId)).ToList();

        var predictionStats = scopedPredictions
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => new
            {
                TotalPoints = g.Sum(p => scoringRules.GetValueOrDefault(p.Outcome!.Value, 0)),
                ExactScores = g.Count(p => p.Outcome == PredictionOutcome.ExactScore),
                CorrectResults = g.Count(p => p.Outcome == PredictionOutcome.CorrectResult),
                Count = g.Select(p => p.GameweekId).Distinct().Count()
            });

        // Include resolved risk play bonus/penalty — scoped to GWs
        var riskBonusByUser = await DbContext.RiskPlays
            .AsNoTracking()
            .Where(r => r.SeasonId == league.SeasonId && r.IsResolved == true
                        && !r.IsDeleted && memberUserIds.Contains(r.UserId)
                        && scopedGwIds.Contains(r.GameweekId))
            .GroupBy(r => r.UserId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(r => r.BonusPoints ?? 0), ct);

        var standings = members
            .Select(m =>
            {
                var stats = predictionStats.GetValueOrDefault(m.UserId);
                var riskBonus = riskBonusByUser.GetValueOrDefault(m.UserId);
                return new LeagueStandingRow(
                    m.UserId,
                    m.DisplayName ?? m.UserIdString,
                    m.AvatarUrl,
                    (stats?.TotalPoints ?? 0) + riskBonus,
                    stats?.ExactScores ?? 0,
                    stats?.CorrectResults ?? 0,
                    stats?.Count ?? 0);
            })
            .OrderByDescending(s => s.TotalPoints)
            .ToList();

        return ScoreCastResponse<LeagueStandingsResult>.Ok(
            new LeagueStandingsResult(league.Name, league.SeasonId, startingGwNumber,
                league.Competition.Name, league.Competition.LogoUrl, standings));
    }
}
