using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;
using ScoreCast.Ws.Application.V1.Config.Queries;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetGlobalDashboardQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetGlobalDashboardQuery, ScoreCastResponse<GlobalDashboardResult>>
{
    public async Task<ScoreCastResponse<GlobalDashboardResult>> ExecuteAsync(GetGlobalDashboardQuery query, CancellationToken ct)
    {
        var competitionCode = !string.IsNullOrEmpty(query.CompetitionCode)
            ? query.CompetitionCode
            : await ResolveCompetitionCode(ct);

        var season = await DbContext.Seasons
            .FirstOrDefaultAsync(s => s.Competition.Code == competitionCode && s.IsCurrent, ct);
        if (season is null)
            return ScoreCastResponse<GlobalDashboardResult>.Error("No current season.");

        var seasonId = season.Id;

        var gw = await DbContext.Gameweeks
            .Where(g => g.SeasonId == seasonId && g.Status != GameweekStatus.Completed)
            .OrderBy(g => g.Number)
            .FirstOrDefaultAsync(ct);

        // 1. Countdown
        DateTime deadline;
        if (gw is null)
            deadline = ScoreCastDateTime.Now.Value;
        else
            deadline = await DbContext.Matches
                .Where(m => m.GameweekId == gw.Id && m.Status == MatchStatus.Scheduled)
                .Select(m => (DateTime?)m.KickoffTime)
                .MinAsync(ct) ?? ScoreCastDateTime.Now.Value;

        var gwPredictions = gw is null ? 0 : await DbContext.Predictions
            .CountAsync(p => p.Match!.GameweekId == gw.Id && p.SeasonId == seasonId && p.PredictionType == PredictionType.Score, ct);

        var gwUsers = gw is null ? 0 : await DbContext.Predictions
            .Where(p => p.Match!.GameweekId == gw.Id && p.SeasonId == seasonId && p.PredictionType == PredictionType.Score)
            .Select(p => p.UserId).Distinct().CountAsync(ct);

        var gwComplete = gw is not null && !await DbContext.Matches
            .AnyAsync(m => m.GameweekId == gw.Id && m.Status != MatchStatus.Finished && m.Status != MatchStatus.Postponed && !m.IsDeleted, ct);

        var countdown = new GameweekCountdown(gw?.Number ?? 0, deadline, gwPredictions, gwUsers, gwComplete);

        // 2. Upcoming match prediction summaries
        var upcomingMatchIds = gw is null ? new List<long>() : await DbContext.Matches
            .Where(m => m.GameweekId == gw.Id && m.Status == MatchStatus.Scheduled)
            .OrderBy(m => m.KickoffTime)
            .Select(m => m.Id)
            .ToListAsync(ct);

        var matchPredictions = await DbContext.Predictions
            .Where(p => p.MatchId.HasValue && upcomingMatchIds.Contains(p.MatchId.Value) && p.PredictionType == PredictionType.Score)
            .Select(p => new { MatchId = p.MatchId!.Value, p.PredictedHomeScore, p.PredictedAwayScore })
            .ToListAsync(ct);

        var matchInfos = await DbContext.Matches
            .Where(m => upcomingMatchIds.Contains(m.Id))
            .Select(m => new { m.Id, HomeName = m.HomeTeam.Name, AwayName = m.AwayTeam.Name, HomeLogo = m.HomeTeam.LogoUrl, AwayLogo = m.AwayTeam.LogoUrl, HomeShort = m.HomeTeam.ShortName, AwayShort = m.AwayTeam.ShortName, KickoffTime = m.KickoffTime ?? ScoreCastDateTime.Now.Value })
            .ToListAsync(ct);

        var upcomingPredictions = new List<MatchPredictionSummary>();
        foreach (var mi in matchInfos.OrderBy(m => m.KickoffTime))
        {
            var preds = matchPredictions.Where(p => p.MatchId == mi.Id).ToList();
            var count = preds.Count;
            if (count == 0)
            {
                upcomingPredictions.Add(new MatchPredictionSummary(mi.Id, mi.HomeName, mi.AwayName, mi.HomeLogo, mi.AwayLogo, mi.KickoffTime, 0, "-", 0, 33, 34, 33, mi.HomeShort, mi.AwayShort));
                continue;
            }

            var grouped = preds.GroupBy(p => $"{p.PredictedHomeScore}-{p.PredictedAwayScore}")
                .OrderByDescending(g => g.Count()).First();
            var mostPct = Math.Round(grouped.Count() * 100.0 / count, 0);

            var homeWins = preds.Count(p => p.PredictedHomeScore > p.PredictedAwayScore);
            var draws = preds.Count(p => p.PredictedHomeScore == p.PredictedAwayScore);
            var awayWins = count - homeWins - draws;

            upcomingPredictions.Add(new MatchPredictionSummary(mi.Id, mi.HomeName, mi.AwayName, mi.HomeLogo, mi.AwayLogo, mi.KickoffTime, count, grouped.Key, mostPct,
                Math.Round(homeWins * 100.0 / count, 0), Math.Round(draws * 100.0 / count, 0), Math.Round(awayWins * 100.0 / count, 0), mi.HomeShort, mi.AwayShort));
        }

        // 3. Global leaderboard — top 5
        var allPreds = await DbContext.Predictions
            .Where(p => p.SeasonId == seasonId && p.PredictionType == PredictionType.Score && p.Outcome != null)
            .Select(p => new { p.UserId, Outcome = p.Outcome!.Value })
            .ToListAsync(ct);

        var scoringRules = await DbContext.PredictionScoringRules
            .Where(r => r.PredictionType == PredictionType.Score)
            .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

        var userScores = allPreds.GroupBy(p => p.UserId)
            .Select(g => new
            {
                UserId = g.Key,
                Points = g.Sum(p => scoringRules.GetValueOrDefault(p.Outcome, 0)),
                Exact = g.Count(p => p.Outcome == PredictionOutcome.ExactScore),
                Total = g.Count()
            })
            .OrderByDescending(u => u.Points)
            .Take(5)
            .ToList();

        var userIds = userScores.Select(u => u.UserId).ToList();
        var userNames = await DbContext.UserMasters
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.DisplayName ?? "Anonymous", ct);

        var topPredictors = userScores.Select((u, i) => new GlobalLeaderboardEntry(
            i + 1, userNames.GetValueOrDefault(u.UserId, "Anonymous"), u.Points, u.Exact, u.Total)).ToList();

        // 4. Community stats
        var totalPredictors = allPreds.Select(p => p.UserId).Distinct().Count();
        var totalPredictions = allPreds.Count;
        var exactScores = allPreds.Count(p => p.Outcome == PredictionOutcome.ExactScore);
        var exactPct = totalPredictions > 0 ? Math.Round(exactScores * 100.0 / totalPredictions, 1) : 0;

        var correctOutcomes = new[] { PredictionOutcome.ExactScore, PredictionOutcome.CorrectResultAndGoalDifference, PredictionOutcome.CorrectResult };

        var finishedPreds = await DbContext.Predictions
            .Where(p => p.SeasonId == seasonId && p.PredictionType == PredictionType.Score && p.Outcome != null && p.Match!.Status == MatchStatus.Finished)
            .Select(p => new { MatchId = p.MatchId!.Value, Outcome = p.Outcome!.Value, HomeName = p.Match!.HomeTeam.Name, AwayName = p.Match.AwayTeam.Name })
            .ToListAsync(ct);

        var matchAccuracy = finishedPreds.GroupBy(p => p.MatchId)
            .Where(g => g.Count() >= 2)
            .Select(g => new { Label = $"{g.First().HomeName} vs {g.First().AwayName}", Accuracy = g.Count(p => correctOutcomes.Contains(p.Outcome)) * 100.0 / g.Count() })
            .ToList();

        var hardest = matchAccuracy.MinBy(m => m.Accuracy);

        var teamAccuracy = finishedPreds
            .SelectMany(p => new[] { (Team: p.HomeName, p.Outcome), (Team: p.AwayName, p.Outcome) })
            .GroupBy(x => x.Team)
            .Where(g => g.Count() >= 5)
            .Select(g => new { Team = g.Key, Pct = g.Count(x => correctOutcomes.Contains(x.Outcome)) * 100.0 / g.Count() })
            .MaxBy(t => t.Pct);

        var community = new CommunityStats(
            totalPredictors, totalPredictions, exactScores, exactPct,
            hardest?.Label ?? "N/A", Math.Round(hardest?.Accuracy ?? 0, 1),
            teamAccuracy?.Team ?? "N/A", Math.Round(teamAccuracy?.Pct ?? 0, 1));

        // 5. Last completed gameweek recap
        GameweekRecap? recap = null;
        var lastCompletedGw = await DbContext.Gameweeks
            .Where(g => g.SeasonId == seasonId && g.Status == GameweekStatus.Completed)
            .OrderByDescending(g => g.Number)
            .FirstOrDefaultAsync(ct);

        if (lastCompletedGw is not null)
        {
            var gwMatchIds = await DbContext.Matches
                .Where(m => m.GameweekId == lastCompletedGw.Id)
                .Select(m => m.Id).ToListAsync(ct);

            var gwPreds2 = await DbContext.Predictions
                .Where(p => p.SeasonId == seasonId && p.PredictionType == PredictionType.Score
                            && p.Outcome != null && p.MatchId.HasValue && gwMatchIds.Contains(p.MatchId.Value))
                .Select(p => new { p.UserId, Outcome = p.Outcome!.Value, p.PredictedHomeScore, p.PredictedAwayScore,
                    HomeScore = p.Match!.HomeScore, AwayScore = p.Match.AwayScore,
                    HomeName = p.Match.HomeTeam.ShortName ?? p.Match.HomeTeam.Name,
                    AwayName = p.Match.AwayTeam.ShortName ?? p.Match.AwayTeam.Name })
                .ToListAsync(ct);

            if (gwPreds2.Count > 0)
            {
                var byUser = gwPreds2.GroupBy(p => p.UserId)
                    .Select(g => new { g.Key, Pts = g.Sum(p => scoringRules.GetValueOrDefault(p.Outcome, 0)) })
                    .OrderByDescending(u => u.Pts).First();
                var bestName = userNames.GetValueOrDefault(byUser.Key, "Anonymous");
                var gwExact = gwPreds2.Count(p => p.Outcome == PredictionOutcome.ExactScore);
                var gwPredictors = gwPreds2.Select(p => p.UserId).Distinct().Count();

                // Biggest upset: match where fewest predicted the correct result
                var correctOutcomes2 = new[] { PredictionOutcome.ExactScore, PredictionOutcome.CorrectResultAndGoalDifference, PredictionOutcome.CorrectResult };
                var upsetMatch = gwPreds2.GroupBy(p => $"{p.HomeName} {p.HomeScore}-{p.AwayScore} {p.AwayName}")
                    .Where(g => g.Count() >= 2)
                    .Select(g => new { Label = g.Key, CorrectPct = g.Count(p => correctOutcomes2.Contains(p.Outcome)) * 100.0 / g.Count() })
                    .MinBy(m => m.CorrectPct);

                // Boldest correct: exact score that fewest predicted
                var boldest = gwPreds2
                    .Where(p => p.Outcome == PredictionOutcome.ExactScore)
                    .GroupBy(p => $"{p.HomeName} {p.PredictedHomeScore}-{p.PredictedAwayScore} {p.AwayName}")
                    .OrderBy(g => g.Count())
                    .Select(g => g.Key)
                    .FirstOrDefault();

                recap = new GameweekRecap(lastCompletedGw.Number, bestName, byUser.Pts, gwExact, gwPredictors,
                    upsetMatch?.Label, boldest);
            }
        }

        return ScoreCastResponse<GlobalDashboardResult>.Ok(
            new GlobalDashboardResult(countdown, upcomingPredictions, topPredictors, community, recap));
    }

    private static async Task<string> ResolveCompetitionCode(CancellationToken ct)
    {
        var config = await new GetAppConfigQuery(AppConfigKeys.DefaultCompetition).ExecuteAsync(ct);
        return config?.RootElement.GetProperty("competitionCode").GetString() ?? CompetitionCodes.PremierLeague;
    }
}
