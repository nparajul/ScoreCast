using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetMatchExtrasQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetMatchExtrasQuery, ScoreCastResponse<MatchExtrasResult>>
{
    public async Task<ScoreCastResponse<MatchExtrasResult>> ExecuteAsync(GetMatchExtrasQuery query, CancellationToken ct)
    {
        var match = await DbContext.Matches.AsNoTracking()
            .Where(m => m.Id == query.MatchId)
            .Select(m => new
            {
                m.HomeTeamId, m.AwayTeamId, m.KickoffTime,
                HomeTeamName = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                HomeLogo = m.HomeTeam.LogoUrl,
                AwayTeamName = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                AwayLogo = m.AwayTeam.LogoUrl,
                SeasonId = m.Gameweek.SeasonId
            })
            .FirstOrDefaultAsync(ct);

        if (match is null)
            return ScoreCastResponse<MatchExtrasResult>.Error("Match not found.");

        var h2hTask = GetH2HAsync(match.HomeTeamId, match.AwayTeamId, query.MatchId, ct);
        var homeFormTask = GetFormAsync(match.HomeTeamId, query.MatchId, match.KickoffTime, ct);
        var awayFormTask = GetFormAsync(match.AwayTeamId, query.MatchId, match.KickoffTime, ct);
        var predictionTask = GetUserPredictionAsync(query.MatchId, query.UserId, ct);
        var communityTask = GetCommunityPredictionsAsync(query.MatchId, ct);
        var homeStatsTask = GetPlayerStatsAsync(match.HomeTeamId, match.SeasonId, ct);
        var awayStatsTask = GetPlayerStatsAsync(match.AwayTeamId, match.SeasonId, ct);

        await Task.WhenAll(h2hTask, homeFormTask, awayFormTask, predictionTask, communityTask, homeStatsTask, awayStatsTask);

        return ScoreCastResponse<MatchExtrasResult>.Ok(new MatchExtrasResult(
            await h2hTask, await homeFormTask, await awayFormTask,
            await predictionTask, await communityTask,
            await homeStatsTask, await awayStatsTask));
    }

    private async Task<List<H2HMatch>> GetH2HAsync(long homeTeamId, long awayTeamId, long excludeMatchId, CancellationToken ct)
    {
        return await DbContext.Matches.AsNoTracking()
            .Where(m => m.Id != excludeMatchId
                && m.Status == MatchStatus.Finished
                && ((m.HomeTeamId == homeTeamId && m.AwayTeamId == awayTeamId)
                    || (m.HomeTeamId == awayTeamId && m.AwayTeamId == homeTeamId)))
            .OrderByDescending(m => m.KickoffTime)
            .Take(6)
            .Select(m => new H2HMatch(
                m.KickoffTime!.Value, m.Id,
                m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                m.HomeTeam.LogoUrl, m.AwayTeam.LogoUrl,
                m.HomeScore ?? 0, m.AwayScore ?? 0))
            .ToListAsync(ct);
    }

    private async Task<List<FormEntry>> GetFormAsync(long teamId, long excludeMatchId, DateTime? beforeKickoff, CancellationToken ct)
    {
        var matches = await DbContext.Matches.AsNoTracking()
            .Where(m => m.Id != excludeMatchId
                && m.Status == MatchStatus.Finished
                && (m.HomeTeamId == teamId || m.AwayTeamId == teamId)
                && (beforeKickoff == null || m.KickoffTime < beforeKickoff))
            .OrderByDescending(m => m.KickoffTime)
            .Take(5)
            .Select(m => new
            {
                m.KickoffTime, m.Id, m.HomeTeamId,
                m.HomeScore, m.AwayScore,
                OpponentName = m.HomeTeamId == teamId
                    ? (m.AwayTeam.ShortName ?? m.AwayTeam.Name)
                    : (m.HomeTeam.ShortName ?? m.HomeTeam.Name),
                OpponentLogo = m.HomeTeamId == teamId ? m.AwayTeam.LogoUrl : m.HomeTeam.LogoUrl
            })
            .ToListAsync(ct);

        return matches.Select(m =>
        {
            var isHome = m.HomeTeamId == teamId;
            var gf = isHome ? m.HomeScore ?? 0 : m.AwayScore ?? 0;
            var ga = isHome ? m.AwayScore ?? 0 : m.HomeScore ?? 0;
            var result = gf > ga ? "W" : gf < ga ? "L" : "D";
            return new FormEntry(m.KickoffTime!.Value, m.Id, m.OpponentName, m.OpponentLogo, isHome, gf, ga, result);
        }).ToList();
    }

    private async Task<UserMatchPrediction?> GetUserPredictionAsync(long matchId, string? firebaseUid, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(firebaseUid)) return null;

        var user = await DbContext.UserMasters.AsNoTracking()
            .Where(u => u.FirebaseUid == firebaseUid)
            .Select(u => new { u.Id })
            .FirstOrDefaultAsync(ct);

        if (user is null) return null;

        var pred = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.MatchId == matchId && p.UserId == user.Id && p.PredictionType == PredictionType.Score && !p.IsDeleted)
            .Select(p => new { p.PredictedHomeScore, p.PredictedAwayScore, p.Outcome })
            .FirstOrDefaultAsync(ct);

        if (pred is null) return null;

        var points = 0;
        if (pred.Outcome is not null)
        {
            var rule = await DbContext.PredictionScoringRules.AsNoTracking()
                .Where(r => r.Outcome == pred.Outcome && r.PredictionType == PredictionType.Score && !r.IsDeleted)
                .Select(r => r.Points)
                .FirstOrDefaultAsync(ct);
            points = rule;
        }

        return new UserMatchPrediction(
            pred.PredictedHomeScore ?? 0, pred.PredictedAwayScore ?? 0,
            pred.Outcome?.ToString(), points);
    }

    private async Task<CommunityPredictions> GetCommunityPredictionsAsync(long matchId, CancellationToken ct)
    {
        var preds = await DbContext.Predictions.AsNoTracking()
            .Where(p => p.MatchId == matchId && p.PredictionType == PredictionType.Score && !p.IsDeleted)
            .Select(p => new { p.PredictedHomeScore, p.PredictedAwayScore })
            .ToListAsync(ct);

        if (preds.Count == 0)
            return new CommunityPredictions(0, 0, 0, 0, null, 0);

        var total = preds.Count;
        var homeWin = preds.Count(p => p.PredictedHomeScore > p.PredictedAwayScore);
        var draw = preds.Count(p => p.PredictedHomeScore == p.PredictedAwayScore);
        var awayWin = total - homeWin - draw;

        var mostPopular = preds
            .GroupBy(p => $"{p.PredictedHomeScore}-{p.PredictedAwayScore}")
            .OrderByDescending(g => g.Count())
            .First();

        return new CommunityPredictions(
            total,
            Pct(homeWin, total), Pct(draw, total), Pct(awayWin, total),
            mostPopular.Key, Pct(mostPopular.Count(), total));
    }

    private async Task<List<PlayerSeasonStat>> GetPlayerStatsAsync(long teamId, long seasonId, CancellationToken ct)
    {
        var teamPlayerIds = await DbContext.TeamPlayers.AsNoTracking()
            .Where(tp => tp.TeamId == teamId && tp.SeasonId == seasonId)
            .Select(tp => tp.PlayerId)
            .ToListAsync(ct);

        if (teamPlayerIds.Count == 0) return [];

        var events = await DbContext.MatchEvents.AsNoTracking()
            .Where(e => !e.IsDeleted
                && teamPlayerIds.Contains(e.PlayerId)
                && e.Match.Gameweek.SeasonId == seasonId)
            .Select(e => new { e.PlayerId, e.EventType })
            .ToListAsync(ct);

        var players = await DbContext.Players.AsNoTracking()
            .Where(p => teamPlayerIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.PhotoUrl, p.Position })
            .ToDictionaryAsync(p => p.Id, ct);

        return events
            .GroupBy(e => e.PlayerId)
            .Select(g =>
            {
                var p = players.GetValueOrDefault(g.Key);
                return new PlayerSeasonStat(
                    g.Key, p?.Name ?? "Unknown", p?.PhotoUrl, p?.Position,
                    g.Count(e => e.EventType is MatchEventType.Goal or MatchEventType.PenaltyGoal),
                    g.Count(e => e.EventType == MatchEventType.Assist),
                    g.Count(e => e.EventType == MatchEventType.YellowCard),
                    g.Count(e => e.EventType == MatchEventType.RedCard));
            })
            .Where(s => s.Goals > 0 || s.Assists > 0)
            .OrderByDescending(s => s.Goals).ThenByDescending(s => s.Assists)
            .Take(10)
            .ToList();
    }

    private static int Pct(int part, int total) => total > 0 ? (int)Math.Round(100.0 * part / total) : 0;
}
