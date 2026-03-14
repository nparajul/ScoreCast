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

        var standings = await DbContext.Predictions
            .AsNoTracking()
            .Where(p => p.PredictionLeagueId == query.PredictionLeagueId)
            .GroupBy(p => p.UserId)
            .Select(g => new LeagueStandingRow(
                g.Key,
                g.First().User.DisplayName ?? g.First().User.UserId,
                g.First().User.AvatarUrl,
                g.Sum(p => p.PointsAwarded),
                g.Count(p => p.PointsAwarded == exactPoints),
                g.Count(p => p.PointsAwarded == correctResultPoints),
                g.Count()))
            .OrderByDescending(s => s.TotalPoints)
            .ToListAsync(ct);

        return ScoreCastResponse<LeagueStandingsResult>.Ok(
            new LeagueStandingsResult(league.Name, standings));
    }
}
