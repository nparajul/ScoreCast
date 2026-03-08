using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetMyPredictionStatsQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetMyPredictionStatsQuery, ScoreCastResponse<MyPredictionStatsResult>>
{
    private static readonly PredictionOutcome[] CorrectOutcomes =
        [PredictionOutcome.ExactScore, PredictionOutcome.CorrectResultAndGoalDifference, PredictionOutcome.CorrectResult];

    public async Task<ScoreCastResponse<MyPredictionStatsResult>> ExecuteAsync(GetMyPredictionStatsQuery query, CancellationToken ct)
    {
        var user = await DbContext.UserMasters.FirstOrDefaultAsync(u => u.FirebaseUid == query.UserId, ct);
        if (user is null)
            return ScoreCastResponse<MyPredictionStatsResult>.Ok(new(0, "", 0, 0, 0, 0, [], null));

        var season = await DbContext.Seasons
            .FirstOrDefaultAsync(s => s.Competition.Code == query.CompetitionCode && s.IsCurrent, ct);
        if (season is null)
            return ScoreCastResponse<MyPredictionStatsResult>.Ok(new(0, "", 0, 0, 0, 0, [], null));

        // All scored predictions for this user+season, ordered by match kickoff
        var myPredictions = await DbContext.Predictions
            .Where(p => p.UserId == user.Id && p.SeasonId == season.Id
                        && p.PredictionType == PredictionType.Score && p.Outcome != null && p.MatchId != null)
            .Select(p => new
            {
                p.MatchId,
                p.Outcome,
                GameweekNumber = p.Match!.Gameweek!.Number,
                KickoffTime = p.Match.KickoffTime
            })
            .OrderBy(p => p.KickoffTime)
            .ToListAsync(ct);

        if (myPredictions.Count == 0)
            return ScoreCastResponse<MyPredictionStatsResult>.Ok(new(0, "", 0, 0, 0, 0, [], null));

        var totalCorrect = myPredictions.Count(p => CorrectOutcomes.Contains(p.Outcome!.Value));
        var totalExact = myPredictions.Count(p => p.Outcome == PredictionOutcome.ExactScore);

        // Streak (consecutive correct results, most recent first)
        var reversed = myPredictions.AsEnumerable().Reverse().ToList();
        var currentStreak = 0;
        var streakType = "";
        foreach (var p in reversed)
        {
            if (CorrectOutcomes.Contains(p.Outcome!.Value))
            {
                currentStreak++;
                if (currentStreak == 1)
                    streakType = p.Outcome == PredictionOutcome.ExactScore ? "exact" : "correct";
            }
            else break;
        }

        // Best streak
        var bestStreak = 0;
        var run = 0;
        foreach (var p in myPredictions)
        {
            if (CorrectOutcomes.Contains(p.Outcome!.Value)) { run++; if (run > bestStreak) bestStreak = run; }
            else run = 0;
        }

        // Achievements
        var achievements = new List<string>();
        if (totalExact >= 1) achievements.Add("🎯 First Exact Score");
        if (totalExact >= 5) achievements.Add("🎯 5 Exact Scores");
        if (totalExact >= 10) achievements.Add("🎯 10 Exact Scores");
        if (bestStreak >= 3) achievements.Add("🔥 3-Match Streak");
        if (bestStreak >= 5) achievements.Add("🔥 5-Match Streak");
        if (bestStreak >= 10) achievements.Add("🔥 10-Match Streak");
        if (myPredictions.Count >= 10) achievements.Add("📊 10 Predictions");
        if (myPredictions.Count >= 50) achievements.Add("📊 50 Predictions");
        if (myPredictions.Count >= 100) achievements.Add("💯 Century Club");
        var incorrectStreak = reversed.TakeWhile(p => p.Outcome == PredictionOutcome.Incorrect).Count();
        if (incorrectStreak == 0 && currentStreak >= 1 && myPredictions.Count >= 5)
            achievements.Add("✅ On Form");

        // You vs Community — last completed gameweek
        GameweekComparison? comparison = null;
        var lastCompletedGw = myPredictions.Max(p => p.GameweekNumber);
        // Find a GW where all matches are finished
        var completedGws = await DbContext.Gameweeks
            .Where(g => g.SeasonId == season.Id && g.Status == GameweekStatus.Completed)
            .OrderByDescending(g => g.Number)
            .Select(g => g.Number)
            .FirstOrDefaultAsync(ct);

        if (completedGws > 0)
        {
            var gwNum = completedGws;
            var myGwPreds = myPredictions.Where(p => p.GameweekNumber == gwNum).ToList();
            if (myGwPreds.Count > 0)
            {
                var userCorrect = myGwPreds.Count(p => CorrectOutcomes.Contains(p.Outcome!.Value));

                // Community stats for this gameweek
                var gwMatchIds = await DbContext.Matches
                    .Where(m => m.Gameweek!.SeasonId == season.Id && m.Gameweek.Number == gwNum)
                    .Select(m => m.Id)
                    .ToListAsync(ct);

                var communityByUser = await DbContext.Predictions
                    .Where(p => p.SeasonId == season.Id && p.PredictionType == PredictionType.Score
                                && p.Outcome != null && gwMatchIds.Contains(p.MatchId!.Value))
                    .GroupBy(p => p.UserId)
                    .Select(g => new
                    {
                        Correct = g.Count(p => CorrectOutcomes.Contains(p.Outcome!.Value)),
                        Total = g.Count()
                    })
                    .ToListAsync(ct);

                if (communityByUser.Count > 1)
                {
                    var avgCorrect = communityByUser.Average(c => c.Correct);
                    var avgTotal = communityByUser.Average(c => c.Total);
                    var beatCount = communityByUser.Count(c => c.Correct < userCorrect);
                    var beatPct = Math.Round(beatCount * 100.0 / communityByUser.Count);

                    comparison = new GameweekComparison(gwNum, userCorrect, myGwPreds.Count,
                        Math.Round(avgCorrect, 1), Math.Round(avgTotal, 1), beatPct);
                }
            }
        }

        return ScoreCastResponse<MyPredictionStatsResult>.Ok(new(
            currentStreak, streakType, bestStreak,
            myPredictions.Count, totalCorrect, totalExact,
            achievements, comparison));
    }
}
