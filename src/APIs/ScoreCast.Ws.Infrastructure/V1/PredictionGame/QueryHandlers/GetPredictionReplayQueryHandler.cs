using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetPredictionReplayQueryHandler(
    IScoreCastDbContext DbContext,
    IChatClient? ChatClient = null) : IQueryHandler<GetPredictionReplayQuery, ScoreCastResponse<PredictionReplayResult>>
{
    private static readonly PredictionOutcome[] CorrectOutcomes =
        [PredictionOutcome.ExactScore, PredictionOutcome.CorrectResultAndGoalDifference, PredictionOutcome.CorrectResult];

    public async Task<ScoreCastResponse<PredictionReplayResult>> ExecuteAsync(GetPredictionReplayQuery query, CancellationToken ct)
    {
        var user = await DbContext.UserMasters.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == query.UserId, ct);
        if (user is null)
            return ScoreCastResponse<PredictionReplayResult>.Error("User not found.");

        var match = await DbContext.Matches.AsNoTracking()
            .Where(m => m.Id == query.MatchId && m.Status == MatchStatus.Finished)
            .Select(m => new
            {
                m.Id, m.HomeScore, m.AwayScore, m.HomeTeamId, m.AwayTeamId,
                HomeTeam = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                AwayTeam = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                HomeLogo = m.HomeTeam.LogoUrl,
                AwayLogo = m.AwayTeam.LogoUrl,
                SeasonId = m.Gameweek.SeasonId
            })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return ScoreCastResponse<PredictionReplayResult>.Error("Match not found or not finished.");

        var prediction = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.MatchId == query.MatchId && p.UserId == user.Id
                && p.PredictionType == PredictionType.Score && !p.IsDeleted)
            .Select(p => new { p.PredictedHomeScore, p.PredictedAwayScore, p.Outcome })
            .FirstOrDefaultAsync(ct);

        if (prediction is null)
            return ScoreCastResponse<PredictionReplayResult>.Error("No prediction found for this match.");

        var points = 0;
        if (prediction.Outcome is not null)
        {
            points = await DbContext.PredictionScoringRules.AsNoTracking()
                .Where(r => r.Outcome == prediction.Outcome && r.PredictionType == PredictionType.Score && !r.IsDeleted)
                .Select(r => r.Points)
                .FirstOrDefaultAsync(ct);
        }

        var goalTimeline = await BuildGoalTimelineAsync(
            query.MatchId, match.HomeTeamId, match.AwayTeamId,
            match.HomeTeam, match.AwayTeam,
            prediction.PredictedHomeScore ?? 0, prediction.PredictedAwayScore ?? 0, ct);

        var deathMinute = FindDeathMinute(goalTimeline,
            prediction.PredictedHomeScore ?? 0, prediction.PredictedAwayScore ?? 0,
            match.HomeScore ?? 0, match.AwayScore ?? 0);

        var rivals = await GetLeagueRivalsAsync(query.MatchId, query.PredictionLeagueId, user.Id, match.SeasonId, ct);
        var accuracy = await GetSeasonAccuracyAsync(user.Id, match.SeasonId, ct);

        string? aiCommentary = null;

        return ScoreCastResponse<PredictionReplayResult>.Ok(new PredictionReplayResult(
            match.Id, user.DisplayName ?? "Player", match.HomeTeam, match.AwayTeam, match.HomeLogo, match.AwayLogo,
            match.HomeScore ?? 0, match.AwayScore ?? 0,
            prediction.PredictedHomeScore ?? 0, prediction.PredictedAwayScore ?? 0,
            prediction.Outcome?.ToString(), points,
            goalTimeline, deathMinute, rivals, accuracy, aiCommentary));
    }

    private async Task<List<ReplayGoalEvent>> BuildGoalTimelineAsync(
        long matchId, long homeTeamId, long awayTeamId,
        string homeTeamName, string awayTeamName,
        int predHome, int predAway, CancellationToken ct)
    {
        var goals = await DbContext.MatchEvents.AsNoTracking()
            .Where(e => e.MatchId == matchId && !e.IsDeleted
                && (e.EventType == MatchEventType.Goal || e.EventType == MatchEventType.PenaltyGoal || e.EventType == MatchEventType.OwnGoal))
            .OrderBy(e => e.Minute)
            .Select(e => new
            {
                e.Minute, e.EventType,
                PlayerName = e.Player.Name,
                e.Player.Id,
                TeamId = DbContext.TeamPlayers
                    .Where(tp => tp.PlayerId == e.Player.Id && tp.SeasonId == DbContext.Matches
                        .Where(m => m.Id == matchId).Select(m => m.Gameweek.SeasonId).FirstOrDefault())
                    .Select(tp => tp.TeamId)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var timeline = new List<ReplayGoalEvent>();
        var runningHome = 0;
        var runningAway = 0;

        foreach (var g in goals)
        {
            var isHomeGoal = g.EventType == MatchEventType.OwnGoal
                ? g.TeamId == awayTeamId
                : g.TeamId == homeTeamId;

            if (isHomeGoal) runningHome++;
            else runningAway++;

            var predResult = GetPredictionResult(predHome, predAway);
            var currentResult = GetPredictionResult(runningHome, runningAway);
            var status = predResult == currentResult ? "alive" : "dead";

            if (runningHome == predHome && runningAway == predAway)
                status = "exact";

            timeline.Add(new ReplayGoalEvent(
                g.Minute ?? "?",
                isHomeGoal ? homeTeamName : awayTeamName,
                g.PlayerName,
                runningHome, runningAway, status));
        }

        return timeline;
    }

    private static string? FindDeathMinute(List<ReplayGoalEvent> timeline, int predHome, int predAway, int actualHome, int actualAway)
    {
        if (predHome == actualHome && predAway == actualAway) return null;

        var predResult = GetPredictionResult(predHome, predAway);
        foreach (var e in timeline)
        {
            var currentResult = GetPredictionResult(e.RunningHome, e.RunningAway);
            if (currentResult != predResult)
                return e.Minute;
        }

        return null;
    }

    private static string GetPredictionResult(int home, int away) =>
        home > away ? "H" : away > home ? "A" : "D";

    private async Task<List<LeagueRivalComparison>> GetLeagueRivalsAsync(
        long matchId, long? leagueId, long userId, long seasonId, CancellationToken ct)
    {
        if (leagueId is null) return [];

        var memberIds = await DbContext.PredictionLeagueMembers.AsNoTracking()
            .Where(m => m.PredictionLeagueId == leagueId && m.UserId != userId)
            .Select(m => m.UserId)
            .ToListAsync(ct);

        if (memberIds.Count == 0) return [];

        var preds = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.MatchId == matchId && memberIds.Contains(p.UserId)
                && p.PredictionType == PredictionType.Score && !p.IsDeleted)
            .Select(p => new
            {
                p.UserId, p.PredictedHomeScore, p.PredictedAwayScore, p.Outcome,
                p.User.DisplayName, p.User.AvatarUrl
            })
            .ToListAsync(ct);

        var rules = await DbContext.PredictionScoringRules.AsNoTracking()
            .Where(r => r.PredictionType == PredictionType.Score && !r.IsDeleted)
            .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

        return preds.Select(p => new LeagueRivalComparison(
            p.DisplayName ?? "Unknown", p.AvatarUrl,
            p.PredictedHomeScore ?? 0, p.PredictedAwayScore ?? 0,
            p.Outcome?.ToString(),
            p.Outcome is not null && rules.TryGetValue(p.Outcome.Value, out var pts) ? pts : 0
        )).OrderByDescending(r => r.Points).ToList();
    }

    private async Task<ReplaySeasonAccuracy> GetSeasonAccuracyAsync(long userId, long seasonId, CancellationToken ct)
    {
        var preds = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.UserId == userId && p.SeasonId == seasonId
                && p.PredictionType == PredictionType.Score && p.Outcome != null && !p.IsDeleted)
            .Select(p => new { p.Outcome, GwNumber = p.Match!.Gameweek.Number })
            .ToListAsync(ct);

        var total = preds.Count;
        var exact = preds.Count(p => p.Outcome == PredictionOutcome.ExactScore);
        var correct = preds.Count(p => CorrectOutcomes.Contains(p.Outcome!.Value));
        var accuracyPct = total > 0 ? (int)Math.Round(100.0 * correct / total) : 0;

        var trend = "steady";
        if (total >= 10)
        {
            var half = total / 2;
            var ordered = preds.OrderBy(p => p.GwNumber).ToList();
            var firstHalf = ordered.Take(half).Count(p => CorrectOutcomes.Contains(p.Outcome!.Value));
            var secondHalf = ordered.Skip(half).Count(p => CorrectOutcomes.Contains(p.Outcome!.Value));
            var firstPct = 100.0 * firstHalf / half;
            var secondPct = 100.0 * secondHalf / (total - half);
            trend = secondPct > firstPct + 5 ? "improving" : secondPct < firstPct - 5 ? "declining" : "steady";
        }

        return new ReplaySeasonAccuracy(total, exact, correct, accuracyPct, trend);
    }

    private async Task<string?> GenerateAiCommentaryAsync(
        string homeTeam, string awayTeam,
        int predHome, int predAway, int actualHome, int actualAway,
        string? outcome, string? deathMinute,
        ReplaySeasonAccuracy accuracy, CancellationToken ct)
    {
        if (ChatClient is null) return null;

        var prompt = $"""
            You are a witty football pundit reviewing someone's match prediction.
            Keep it to 2 sentences max. Be funny, use football banter. No emojis.

            Match: {homeTeam} {actualHome}-{actualAway} {awayTeam}
            Their prediction: {homeTeam} {predHome}-{predAway} {awayTeam}
            Outcome: {outcome ?? "Unknown"}
            {(deathMinute is not null ? $"Their prediction died at minute {deathMinute}." : "They nailed it.")}
            Season accuracy: {accuracy.AccuracyPct}% correct ({accuracy.ExactScores} exact scores from {accuracy.TotalPredictions} predictions)
            Trend: {accuracy.Trend}
            """;

        try
        {
            var response = await ChatClient.GetResponseAsync(prompt, cancellationToken: ct);
            return response.Text?.Trim();
        }
        catch
        {
            return null;
        }
    }
}
