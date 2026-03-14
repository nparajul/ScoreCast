using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetLeagueStandingsQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetLeagueStandingsQuery, ScoreCastResponse<LeagueStandingsResult>>
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
            .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

        var exactPoints = scoringRules.GetValueOrDefault(PredictionOutcome.ExactScore, 10);
        var correctResultPoints = scoringRules.GetValueOrDefault(PredictionOutcome.CorrectResult, 5);

        var members = await DbContext.PredictionLeagueMembers
            .AsNoTracking()
            .Where(m => m.PredictionLeagueId == query.PredictionLeagueId)
            .Select(m => new { m.UserId, m.User.DisplayName, m.User.AvatarUrl, UserIdString = m.User.UserId })
            .ToListAsync(ct);

        var predictionStats = await DbContext.Predictions
            .AsNoTracking()
            .Where(p => p.PredictionLeagueId == query.PredictionLeagueId)
            .GroupBy(p => p.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                TotalPoints = g.Sum(p => p.PointsAwarded),
                ExactScores = g.Count(p => p.PointsAwarded == exactPoints),
                CorrectResults = g.Count(p => p.PointsAwarded == correctResultPoints),
                Count = g.Count()
            })
            .ToDictionaryAsync(s => s.UserId, ct);

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
