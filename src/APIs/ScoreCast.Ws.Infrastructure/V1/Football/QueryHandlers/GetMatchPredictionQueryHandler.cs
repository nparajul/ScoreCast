using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetMatchPredictionQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetMatchPredictionQuery, ScoreCastResponse<MatchPredictionResult>>
{
    private const int FormWindow = 10;
    private const double LeagueAvgGoals = 1.35; // baseline per team per match
    private const double HomeAdvantage = 0.15;
    private const int MaxGoals = 6;

    public async Task<ScoreCastResponse<MatchPredictionResult>> ExecuteAsync(
        GetMatchPredictionQuery query, CancellationToken ct)
    {
        var match = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Id == query.MatchId)
            .Select(m => new { m.HomeTeamId, m.AwayTeamId, m.Gameweek.SeasonId })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return ScoreCastResponse<MatchPredictionResult>.Error("Match not found");

        // Get all finished matches this season
        var results = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == match.SeasonId && m.Status == MatchStatus.Finished
                        && m.HomeScore != null && m.AwayScore != null)
            .Select(m => new MatchResult(m.HomeTeamId, m.AwayTeamId, m.HomeScore!.Value, m.AwayScore!.Value))
            .ToListAsync(ct);

        if (results.Count < 10)
            return ScoreCastResponse<MatchPredictionResult>.Error("Not enough data");

        // League averages
        var avgHomeGoals = results.Average(r => (double)r.HomeScore);
        var avgAwayGoals = results.Average(r => (double)r.AwayScore);

        // Team attack/defence strengths (last N matches)
        var homeTeamMatches = results
            .Where(r => r.HomeTeamId == match.HomeTeamId || r.AwayTeamId == match.HomeTeamId)
            .TakeLast(FormWindow).ToList();
        var awayTeamMatches = results
            .Where(r => r.HomeTeamId == match.AwayTeamId || r.AwayTeamId == match.AwayTeamId)
            .TakeLast(FormWindow).ToList();

        var homeAttack = CalcStrength(homeTeamMatches, match.HomeTeamId, avgHomeGoals, avgAwayGoals, true);
        var homeDefence = CalcStrength(homeTeamMatches, match.HomeTeamId, avgHomeGoals, avgAwayGoals, false);
        var awayAttack = CalcStrength(awayTeamMatches, match.AwayTeamId, avgHomeGoals, avgAwayGoals, true);
        var awayDefence = CalcStrength(awayTeamMatches, match.AwayTeamId, avgHomeGoals, avgAwayGoals, false);

        // H2H adjustment
        var h2h = results
            .Where(r => (r.HomeTeamId == match.HomeTeamId && r.AwayTeamId == match.AwayTeamId)
                     || (r.HomeTeamId == match.AwayTeamId && r.AwayTeamId == match.HomeTeamId))
            .TakeLast(5).ToList();

        var h2hAdj = 0.0;
        if (h2h.Count >= 2)
        {
            var homeH2hGoals = h2h.Average(r =>
                r.HomeTeamId == match.HomeTeamId ? (double)r.HomeScore : (double)r.AwayScore);
            var awayH2hGoals = h2h.Average(r =>
                r.HomeTeamId == match.AwayTeamId ? (double)r.HomeScore : (double)r.AwayScore);
            h2hAdj = (homeH2hGoals - awayH2hGoals) * 0.1;
        }

        // Expected goals (Poisson lambda)
        var homeLambda = Math.Max(0.3, homeAttack * awayDefence * avgHomeGoals + HomeAdvantage + h2hAdj);
        var awayLambda = Math.Max(0.3, awayAttack * homeDefence * avgAwayGoals - h2hAdj * 0.5);

        // Cap lambdas to reasonable range
        homeLambda = Math.Min(homeLambda, 4.0);
        awayLambda = Math.Min(awayLambda, 4.0);

        // Build probability matrix via Poisson
        var matrix = new double[MaxGoals + 1, MaxGoals + 1];
        double homeWin = 0, draw = 0, awayWin = 0;

        for (var h = 0; h <= MaxGoals; h++)
        {
            for (var a = 0; a <= MaxGoals; a++)
            {
                var p = Poisson(h, homeLambda) * Poisson(a, awayLambda);
                matrix[h, a] = p;
                if (h > a) homeWin += p;
                else if (h == a) draw += p;
                else awayWin += p;
            }
        }

        var total = homeWin + draw + awayWin;
        var homeWinPct = (int)Math.Round(homeWin / total * 100);
        var awayWinPct = (int)Math.Round(awayWin / total * 100);
        var drawPct = 100 - homeWinPct - awayWinPct;

        // Top scorelines
        var scorelines = new List<(int H, int A, double P)>();
        for (var h = 0; h <= MaxGoals; h++)
            for (var a = 0; a <= MaxGoals; a++)
                scorelines.Add((h, a, matrix[h, a]));

        var topScorelines = scorelines
            .OrderByDescending(s => s.P)
            .Take(6)
            .Select(s => new ScorelineProbability(s.H, s.A, (int)Math.Round(s.P / total * 100)))
            .ToList();

        return ScoreCastResponse<MatchPredictionResult>.Ok(new MatchPredictionResult(
            Math.Round(homeLambda, 2), Math.Round(awayLambda, 2),
            homeWinPct, drawPct, awayWinPct, topScorelines));
    }

    private sealed record MatchResult(long HomeTeamId, long AwayTeamId, int HomeScore, int AwayScore);

    private static double CalcStrength(
        List<MatchResult> matches, long teamId, double avgHome, double avgAway, bool attack)
    {
        if (matches.Count == 0) return 1.0;
        var goals = matches.Average(m =>
        {
            var isHome = m.HomeTeamId == teamId;
            return attack
                ? (double)(isHome ? m.HomeScore : m.AwayScore)
                : (double)(isHome ? m.AwayScore : m.HomeScore);
        });
        var avg = matches.Average(m =>
        {
            var isHome = m.HomeTeamId == teamId;
            return attack
                ? (isHome ? avgHome : avgAway)
                : (isHome ? avgAway : avgHome);
        });
        return avg > 0 ? goals / avg : 1.0;
    }

    private static double Poisson(int k, double lambda)
    {
        // P(X=k) = (lambda^k * e^-lambda) / k!
        var logP = k * Math.Log(lambda) - lambda - LogFactorial(k);
        return Math.Exp(logP);
    }

    private static double LogFactorial(int n)
    {
        double result = 0;
        for (var i = 2; i <= n; i++) result += Math.Log(i);
        return result;
    }
}
