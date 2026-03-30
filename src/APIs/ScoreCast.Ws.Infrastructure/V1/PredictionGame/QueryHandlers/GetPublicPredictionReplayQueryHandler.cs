using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetPublicPredictionReplayQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetPublicPredictionReplayQuery, ScoreCastResponse<PredictionReplayResult>>
{
    public async Task<ScoreCastResponse<PredictionReplayResult>> ExecuteAsync(GetPublicPredictionReplayQuery query, CancellationToken ct)
    {
        var userExists = await DbContext.UserMasters.AsNoTracking().AnyAsync(u => u.Id == query.UserId, ct);
        if (!userExists)
            return ScoreCastResponse<PredictionReplayResult>.Error("User not found.");

        var match = await DbContext.Matches.AsNoTracking()
            .Where(m => m.Id == query.MatchId && m.Status == MatchStatus.Finished)
            .Select(m => new
            {
                m.Id, m.HomeScore, m.AwayScore, m.HomeTeamId, m.AwayTeamId,
                HomeTeam = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                AwayTeam = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                HomeLogo = m.HomeTeam.LogoUrl, AwayLogo = m.AwayTeam.LogoUrl,
                SeasonId = m.Gameweek.SeasonId
            })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return ScoreCastResponse<PredictionReplayResult>.Error("Match not found.");

        var prediction = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.MatchId == query.MatchId && p.UserId == query.UserId
                && p.PredictionType == PredictionType.Score && !p.IsDeleted)
            .Select(p => new { p.PredictedHomeScore, p.PredictedAwayScore, p.Outcome })
            .FirstOrDefaultAsync(ct);

        if (prediction is null)
            return ScoreCastResponse<PredictionReplayResult>.Error("No prediction found.");

        var points = prediction.Outcome is not null
            ? await DbContext.PredictionScoringRules.AsNoTracking()
                .Where(r => r.Outcome == prediction.Outcome && r.PredictionType == PredictionType.Score && !r.IsDeleted)
                .Select(r => r.Points).FirstOrDefaultAsync(ct)
            : 0;

        var goals = await DbContext.MatchEvents.AsNoTracking()
            .Where(e => e.MatchId == query.MatchId && !e.IsDeleted
                && (e.EventType == MatchEventType.Goal || e.EventType == MatchEventType.PenaltyGoal || e.EventType == MatchEventType.OwnGoal))
            .OrderBy(e => e.Minute)
            .Select(e => new { e.Minute, e.EventType, e.Player.Name,
                TeamId = DbContext.TeamPlayers
                    .Where(tp => tp.PlayerId == e.Player.Id && tp.SeasonId == match.SeasonId)
                    .Select(tp => tp.TeamId).FirstOrDefault() })
            .ToListAsync(ct);

        var timeline = new List<ReplayGoalEvent>();
        var rh = 0; var ra = 0;
        var predResult = GetResult(prediction.PredictedHomeScore ?? 0, prediction.PredictedAwayScore ?? 0);
        foreach (var g in goals)
        {
            var isHome = g.EventType == MatchEventType.OwnGoal ? g.TeamId == match.AwayTeamId : g.TeamId == match.HomeTeamId;
            if (isHome) rh++; else ra++;
            var status = rh == (prediction.PredictedHomeScore ?? 0) && ra == (prediction.PredictedAwayScore ?? 0) ? "exact"
                : GetResult(rh, ra) == predResult ? "alive" : "dead";
            timeline.Add(new ReplayGoalEvent(g.Minute ?? "?", isHome ? match.HomeTeam : match.AwayTeam, g.Name, rh, ra, status));
        }

        string? deathMinute = null;
        if ((prediction.PredictedHomeScore ?? 0) != (match.HomeScore ?? 0) || (prediction.PredictedAwayScore ?? 0) != (match.AwayScore ?? 0))
            deathMinute = timeline.FirstOrDefault(e => GetResult(e.RunningHome, e.RunningAway) != predResult)?.Minute;

        var accuracy = await GetAccuracyAsync(query.UserId, match.SeasonId, ct);

        return ScoreCastResponse<PredictionReplayResult>.Ok(new PredictionReplayResult(
            match.Id, match.HomeTeam, match.AwayTeam, match.HomeLogo, match.AwayLogo,
            match.HomeScore ?? 0, match.AwayScore ?? 0,
            prediction.PredictedHomeScore ?? 0, prediction.PredictedAwayScore ?? 0,
            prediction.Outcome?.ToString(), points,
            timeline, deathMinute, [], accuracy, null));
    }

    private static string GetResult(int h, int a) => h > a ? "H" : a > h ? "A" : "D";

    private static readonly PredictionOutcome[] CorrectOutcomes =
        [PredictionOutcome.ExactScore, PredictionOutcome.CorrectResultAndGoalDifference, PredictionOutcome.CorrectResult];

    private async Task<ReplaySeasonAccuracy> GetAccuracyAsync(long userId, long seasonId, CancellationToken ct)
    {
        var preds = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.UserId == userId && p.SeasonId == seasonId
                && p.PredictionType == PredictionType.Score && p.Outcome != null && !p.IsDeleted)
            .Select(p => new { p.Outcome, GwNumber = p.Match!.Gameweek.Number })
            .ToListAsync(ct);

        var total = preds.Count;
        var exact = preds.Count(p => p.Outcome == PredictionOutcome.ExactScore);
        var correct = preds.Count(p => CorrectOutcomes.Contains(p.Outcome!.Value));
        var pct = total > 0 ? (int)Math.Round(100.0 * correct / total) : 0;

        var trend = "steady";
        if (total >= 10)
        {
            var half = total / 2;
            var ordered = preds.OrderBy(p => p.GwNumber).ToList();
            var f = ordered.Take(half).Count(p => CorrectOutcomes.Contains(p.Outcome!.Value));
            var s = ordered.Skip(half).Count(p => CorrectOutcomes.Contains(p.Outcome!.Value));
            var fp = 100.0 * f / half;
            var sp = 100.0 * s / (total - half);
            trend = sp > fp + 5 ? "improving" : sp < fp - 5 ? "declining" : "steady";
        }

        return new ReplaySeasonAccuracy(total, exact, correct, pct, trend);
    }
}
