using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.CommandHandlers;

internal sealed record CalculateOutcomesCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<CalculateOutcomesCommand, ScoreCastResponse>
{
    // Risk play bonus/penalty points
    private const int DoubleDownPenalty = -5;
    private const int ExactBoostBonus = 15;
    private const int ExactBoostPenalty = -5;
    private const int CleanSheetBonus = 5;
    private const int CleanSheetPenalty = -3;
    private const int FirstGoalBonus = 3;
    private const int FirstGoalPenalty = -2;
    private const int OverUnderBonus = 3;
    private const int OverUnderPenalty = -2;

    public async Task<ScoreCastResponse> ExecuteAsync(CalculateOutcomesCommand command, CancellationToken ct)
    {
        var scoringRules = await DbContext.PredictionScoringRules
            .AsNoTracking()
            .Where(r => r.PredictionType == PredictionType.Score && r.StageType == null)
            .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

        // 1. Resolve prediction outcomes (unchanged)
        var predictions = await DbContext.Predictions
            .Include(p => p.Match)
            .Where(p => p.SeasonId == command.Request.SeasonId
                        && p.PredictionType == PredictionType.Score
                        && p.Outcome == null
                        && p.Match!.Status == MatchStatus.Finished
                        && p.Match.HomeScore != null && p.Match.AwayScore != null)
            .ToListAsync(ct);

        foreach (var prediction in predictions)
        {
            prediction.Outcome = DetermineOutcome(
                prediction.PredictedHomeScore!.Value, prediction.PredictedAwayScore!.Value,
                prediction.Match!.HomeScore!.Value, prediction.Match.AwayScore!.Value);
        }

        // 2. Resolve risk plays — both live (preview) and finished (final)
        var riskPlays = await DbContext.RiskPlays
            .Include(r => r.Match)
            .Where(r => r.SeasonId == command.Request.SeasonId && !r.IsDeleted
                        && r.Match.Status != MatchStatus.Scheduled
                        && r.Match.HomeScore != null && r.Match.AwayScore != null)
            .ToListAsync(ct);

        if (riskPlays.Count > 0)
            await ResolveRiskPlaysAsync(riskPlays, command.Request.SeasonId, scoringRules, ct);

        // 3. Update user totals
        if (predictions.Count > 0)
        {
            var pointsByUser = predictions
                .GroupBy(p => p.UserId)
                .ToDictionary(g => g.Key, g => g.Sum(p => scoringRules.GetValueOrDefault(p.Outcome!.Value)));

            var userIds = pointsByUser.Keys.ToList();
            var users = await DbContext.UserMasters
                .Where(u => userIds.Contains(u.Id))
                .ToListAsync(ct);

            foreach (var user in users)
                user.TotalPoints += pointsByUser[user.Id];

            // Recalculate best gameweek (including resolved risk play bonus)
            foreach (var user in users)
            {
                var gwPredPoints = await DbContext.Predictions
                    .Where(p => p.UserId == user.Id && p.Outcome != null)
                    .GroupBy(p => new { p.SeasonId, p.Match!.GameweekId })
                    .Select(g => new { g.Key.GameweekId, Points = g.Sum(p => scoringRules.GetValueOrDefault(p.Outcome!.Value)) })
                    .ToListAsync(ct);

                var gwRiskPoints = await DbContext.RiskPlays
                    .Where(r => r.UserId == user.Id && r.IsResolved == true && !r.IsDeleted)
                    .GroupBy(r => r.GameweekId)
                    .Select(g => new { GameweekId = g.Key, Points = g.Sum(r => r.BonusPoints ?? 0) })
                    .ToListAsync(ct);

                var allGwPoints = gwPredPoints
                    .GroupJoin(gwRiskPoints, p => p.GameweekId, r => r.GameweekId,
                        (p, risks) => p.Points + risks.Sum(r => r.Points))
                    .ToList();

                user.BestGameweek = allGwPoints.Count > 0 ? allGwPoints.Max() : 0;

                // Add resolved risk play bonus to total
                var resolvedRiskBonus = riskPlays
                    .Where(r => r.UserId == user.Id && r.IsResolved == true)
                    .Sum(r => r.BonusPoints ?? 0);
                user.TotalPoints += resolvedRiskBonus;
            }
        }

        await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(CalculateOutcomesCommand), ct);

        return ScoreCastResponse.Ok($"Updated {predictions.Count} predictions, {riskPlays.Count} risk plays");
    }

    private async Task ResolveRiskPlaysAsync(
        List<RiskPlay> riskPlays, long seasonId,
        Dictionary<PredictionOutcome, int> scoringRules, CancellationToken ct)
    {
        // Load predictions for risk play matches to check outcomes
        var riskMatchIds = riskPlays.Select(r => r.MatchId).Distinct().ToList();
        var riskUserIds = riskPlays.Select(r => r.UserId).Distinct().ToList();

        var predictionLookup = await DbContext.Predictions
            .AsNoTracking()
            .Where(p => p.SeasonId == seasonId && riskUserIds.Contains(p.UserId)
                        && p.MatchId != null && riskMatchIds.Contains(p.MatchId.Value))
            .ToListAsync(ct);
        var predByUserMatch = predictionLookup.ToDictionary(p => (p.UserId, p.MatchId!.Value));

        // For FirstGoalTeam: get earliest goal events per match
        var goalEvents = await DbContext.MatchEvents
            .AsNoTracking()
            .Where(e => riskMatchIds.Contains(e.MatchId)
                        && (e.EventType == MatchEventType.Goal || e.EventType == MatchEventType.PenaltyGoal))
            .OrderBy(e => e.Minute)
            .ToListAsync(ct);

        // Player → Team mapping
        var playerIds = goalEvents.Select(e => e.PlayerId).Distinct().ToList();
        var playerTeams = await DbContext.TeamPlayers
            .AsNoTracking()
            .Where(tp => tp.SeasonId == seasonId && playerIds.Contains(tp.PlayerId))
            .ToDictionaryAsync(tp => tp.PlayerId, tp => tp.TeamId, ct);

        var firstGoalTeamByMatch = goalEvents
            .GroupBy(e => e.MatchId)
            .ToDictionary(g => g.Key, g =>
            {
                var first = g.First();
                return playerTeams.GetValueOrDefault(first.PlayerId);
            });

        foreach (var rp in riskPlays)
        {
            var match = rp.Match;
            var isFinished = match.Status == MatchStatus.Finished;
            var homeScore = match.HomeScore!.Value;
            var awayScore = match.AwayScore!.Value;

            var (won, bonus) = rp.RiskType switch
            {
                RiskPlayType.DoubleDown => ResolveDoubleDown(rp, predByUserMatch, scoringRules),
                RiskPlayType.ExactScoreBoost => ResolveExactScoreBoost(rp, predByUserMatch),
                RiskPlayType.CleanSheetBet => ResolveCleanSheet(rp, homeScore, awayScore),
                RiskPlayType.FirstGoalTeam => ResolveFirstGoalTeam(rp, homeScore, awayScore, firstGoalTeamByMatch),
                RiskPlayType.OverUnderGoals => ResolveOverUnder(rp, homeScore, awayScore),
                _ => (false, 0)
            };

            rp.IsWon = won;
            rp.BonusPoints = bonus;
            rp.IsResolved = isFinished; // Only finalize at FT
        }
    }

    private static (bool Won, int Bonus) ResolveDoubleDown(
        RiskPlay rp, Dictionary<(long, long), Prediction> preds,
        Dictionary<PredictionOutcome, int> rules)
    {
        if (!preds.TryGetValue((rp.UserId, rp.MatchId), out var pred) || pred.Outcome is null)
            return (false, 0);

        var basePoints = rules.GetValueOrDefault(pred.Outcome.Value);
        if (pred.Outcome == PredictionOutcome.Incorrect)
            return (false, DoubleDownPenalty);

        return (true, basePoints); // Double = base + base (bonus = extra base)
    }

    private static (bool Won, int Bonus) ResolveExactScoreBoost(
        RiskPlay rp, Dictionary<(long, long), Prediction> preds)
    {
        if (!preds.TryGetValue((rp.UserId, rp.MatchId), out var pred))
            return (false, ExactBoostPenalty);

        // Check current match score vs prediction (works for live + finished)
        var match = rp.Match;
        if (pred.PredictedHomeScore == match.HomeScore && pred.PredictedAwayScore == match.AwayScore)
            return (true, ExactBoostBonus);

        return (false, ExactBoostPenalty);
    }

    private static (bool Won, int Bonus) ResolveCleanSheet(RiskPlay rp, int homeScore, int awayScore)
    {
        // Selection = home team ID or away team ID as string
        if (!long.TryParse(rp.Selection, out var teamId)) return (false, CleanSheetPenalty);

        var conceded = teamId == rp.Match.HomeTeamId ? awayScore : homeScore;
        return conceded == 0 ? (true, CleanSheetBonus) : (false, CleanSheetPenalty);
    }

    private static (bool Won, int Bonus) ResolveFirstGoalTeam(
        RiskPlay rp, int homeScore, int awayScore,
        Dictionary<long, long> firstGoalTeamByMatch)
    {
        if (!long.TryParse(rp.Selection, out var selectedTeamId)) return (false, FirstGoalPenalty);

        // No goals yet — not won, no penalty yet (will resolve at FT)
        if (homeScore + awayScore == 0)
            return (false, 0);

        if (firstGoalTeamByMatch.TryGetValue(rp.MatchId, out var scoringTeamId))
            return scoringTeamId == selectedTeamId ? (true, FirstGoalBonus) : (false, FirstGoalPenalty);

        return (false, 0); // No event data yet
    }

    private static (bool Won, int Bonus) ResolveOverUnder(RiskPlay rp, int homeScore, int awayScore)
    {
        var totalGoals = homeScore + awayScore;
        var isOver = rp.Selection?.Equals("Over", StringComparison.OrdinalIgnoreCase) == true;

        if (isOver)
            return totalGoals > 2 ? (true, OverUnderBonus) : (false, OverUnderPenalty);
        else
            return totalGoals < 3 ? (true, OverUnderBonus) : (false, OverUnderPenalty);
    }

    private static PredictionOutcome DetermineOutcome(int predHome, int predAway, int actualHome, int actualAway)
    {
        if (predHome == actualHome && predAway == actualAway)
            return PredictionOutcome.ExactScore;

        var predDiff = predHome - predAway;
        var actualDiff = actualHome - actualAway;
        var predResult = Math.Sign(predDiff);
        var actualResult = Math.Sign(actualDiff);

        if (predResult == actualResult && predDiff == actualDiff)
            return PredictionOutcome.CorrectResultAndGoalDifference;

        if (predResult == actualResult)
            return PredictionOutcome.CorrectResult;

        if (predDiff == actualDiff)
            return PredictionOutcome.CorrectGoalDifference;

        return PredictionOutcome.Incorrect;
    }
}
