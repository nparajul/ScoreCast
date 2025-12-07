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
            .FirstOrDefaultAsync(l => l.Id == query.PredictionLeagueId, ct);

        if (league is null)
            return ScoreCastResponse<LeagueStandingsResult>.Error("League not found");

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
            .Select(p => new { p.UserId, p.Outcome })
            .ToListAsync(ct);

        var predictionStats = predictions
            .GroupBy(p => p.UserId)
            .ToDictionary(g => g.Key, g => new
            {
                TotalPoints = g.Sum(p => scoringRules.GetValueOrDefault(p.Outcome!.Value, 0)),
                ExactScores = g.Count(p => p.Outcome == PredictionOutcome.ExactScore),
                CorrectResults = g.Count(p => p.Outcome == PredictionOutcome.CorrectResult),
                Count = g.Count()
            });

        var standings = members
            .Select(m =>
            {
                var stats = predictionStats.GetValueOrDefault(m.UserId);
                return new LeagueStandingRow(
                    m.UserId,
                    m.DisplayName ?? m.UserIdString,
                    m.AvatarUrl,
                    stats?.TotalPoints ?? 0,
                    stats?.ExactScores ?? 0,
                    stats?.CorrectResults ?? 0,
                    stats?.Count ?? 0);
            })
            .OrderByDescending(s => s.TotalPoints)
            .ToList();

        return ScoreCastResponse<LeagueStandingsResult>.Ok(
            new LeagueStandingsResult(league.Name, standings));
    }
}
