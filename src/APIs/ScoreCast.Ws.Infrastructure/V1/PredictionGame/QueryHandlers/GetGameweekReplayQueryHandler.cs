using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetGameweekReplayQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetGameweekReplayQuery, ScoreCastResponse<GameweekReplayResult>>
{
    private static readonly PredictionOutcome[] CorrectOutcomes =
        [PredictionOutcome.ExactScore, PredictionOutcome.CorrectResultAndGoalDifference, PredictionOutcome.CorrectResult];

    public async Task<ScoreCastResponse<GameweekReplayResult>> ExecuteAsync(GetGameweekReplayQuery query, CancellationToken ct)
    {
        var user = await DbContext.UserMasters.AsNoTracking()
            .Where(u => u.Id == query.UserId)
            .Select(u => new { u.Id, u.DisplayName })
            .FirstOrDefaultAsync(ct);

        if (user is null)
            return ScoreCastResponse<GameweekReplayResult>.Error("User not found.");

        var season = await DbContext.Seasons.AsNoTracking()
            .Where(s => s.Id == query.SeasonId)
            .Select(s => new { s.CompetitionId, CompetitionName = s.Competition.Name, CompetitionLogo = s.Competition.LogoUrl })
            .FirstOrDefaultAsync(ct);

        if (season is null)
            return ScoreCastResponse<GameweekReplayResult>.Error("Season not found.");

        var matches = await DbContext.Matches.AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == query.SeasonId && m.Gameweek.Number == query.GameweekNumber
                && m.Status == MatchStatus.Finished && !m.IsDeleted)
            .Select(m => new
            {
                m.Id, m.HomeScore, m.AwayScore, m.HomeTeamId, m.AwayTeamId,
                Home = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                Away = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                HomeLogo = m.HomeTeam.LogoUrl, AwayLogo = m.AwayTeam.LogoUrl,
                SeasonId = m.Gameweek.SeasonId
            })
            .ToListAsync(ct);

        var matchIds = matches.Select(m => m.Id).ToList();

        var predictions = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.UserId == user.Id && matchIds.Contains(p.MatchId!.Value)
                && p.PredictionType == PredictionType.Score && !p.IsDeleted)
            .Select(p => new { p.MatchId, p.PredictedHomeScore, p.PredictedAwayScore, p.Outcome })
            .ToDictionaryAsync(p => p.MatchId!.Value, ct);

        var rules = await DbContext.PredictionScoringRules.AsNoTracking()
            .Where(r => r.PredictionType == PredictionType.Score && !r.IsDeleted)
            .ToDictionaryAsync(r => r.Outcome, r => r.Points, ct);

        // Get all goals for death minute calculation
        var goals = await DbContext.MatchEvents.AsNoTracking()
            .Where(e => matchIds.Contains(e.MatchId) && !e.IsDeleted
                && (e.EventType == MatchEventType.Goal || e.EventType == MatchEventType.PenaltyGoal || e.EventType == MatchEventType.OwnGoal))
            .OrderBy(e => e.Minute)
            .Select(e => new
            {
                e.MatchId, e.Minute, e.EventType,
                TeamId = DbContext.TeamPlayers
                    .Where(tp => tp.PlayerId == e.Player.Id && tp.SeasonId == query.SeasonId)
                    .Select(tp => tp.TeamId).FirstOrDefault()
            })
            .ToListAsync(ct);

        var goalsByMatch = goals.GroupBy(g => g.MatchId).ToDictionary(g => g.Key, g => g.ToList());

        var replayMatches = new List<GameweekReplayMatch>();
        var totalPoints = 0;

        foreach (var m in matches)
        {
            if (!predictions.TryGetValue(m.Id, out var pred)) continue;

            var pts = pred.Outcome is not null && rules.TryGetValue(pred.Outcome.Value, out var p) ? p : 0;
            totalPoints += pts;

            string? deathMinute = null;
            var ph = pred.PredictedHomeScore ?? 0;
            var pa = pred.PredictedAwayScore ?? 0;
            var ah = m.HomeScore ?? 0;
            var aa = m.AwayScore ?? 0;

            if (ph != ah || pa != aa)
            {
                var predResult = ph > pa ? "H" : pa > ph ? "A" : "D";
                if (goalsByMatch.TryGetValue(m.Id, out var matchGoals))
                {
                    var rh = 0; var ra = 0;
                    foreach (var g in matchGoals)
                    {
                        var isHome = g.EventType == MatchEventType.OwnGoal ? g.TeamId == m.AwayTeamId : g.TeamId == m.HomeTeamId;
                        if (isHome) rh++; else ra++;
                        var curResult = rh > ra ? "H" : ra > rh ? "A" : "D";
                        if (curResult != predResult) { deathMinute = g.Minute; break; }
                    }
                }
            }

            replayMatches.Add(new GameweekReplayMatch(
                m.Id, m.Home, m.Away, m.HomeLogo, m.AwayLogo, ah, aa, ph, pa,
                pred.Outcome?.ToString(), pts, deathMinute));
        }

        var predicted = replayMatches.Count;
        var correct = replayMatches.Count(m => m.Outcome is not null && CorrectOutcomes.Contains(Enum.Parse<PredictionOutcome>(m.Outcome)));
        var exact = replayMatches.Count(m => m.Outcome == nameof(PredictionOutcome.ExactScore));

        return ScoreCastResponse<GameweekReplayResult>.Ok(new GameweekReplayResult(
            user.DisplayName ?? "Player", query.GameweekNumber,
            season.CompetitionName, season.CompetitionLogo,
            totalPoints, predicted, correct, exact, replayMatches));
    }
}
