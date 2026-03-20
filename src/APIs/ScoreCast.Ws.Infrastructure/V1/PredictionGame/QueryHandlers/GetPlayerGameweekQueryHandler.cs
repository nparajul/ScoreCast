using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Queries;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.QueryHandlers;

internal sealed record GetPlayerGameweekQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetPlayerGameweekQuery, ScoreCastResponse<PlayerGameweekResult>>
{
    public async Task<ScoreCastResponse<PlayerGameweekResult>> ExecuteAsync(GetPlayerGameweekQuery query, CancellationToken ct)
    {
        // Verify requesting user is in the league
        var requestingUser = await DbContext.UserMasters.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == query.RequestingUserId, ct);
        if (requestingUser is null)
            return ScoreCastResponse<PlayerGameweekResult>.Error("Unauthorized");

        var isMember = await DbContext.PredictionLeagueMembers.AsNoTracking()
            .AnyAsync(m => m.PredictionLeagueId == query.PredictionLeagueId && m.UserId == requestingUser.Id, ct);
        if (!isMember)
            return ScoreCastResponse<PlayerGameweekResult>.Error("Not a member of this league");

        // Get matches for this gameweek with kickoff times
        var matches = await DbContext.Matches.AsNoTracking()
            .Where(m => m.GameweekId == query.GameweekId)
            .Select(m => new { m.Id, m.KickoffTime, m.Status })
            .ToListAsync(ct);

        var matchIds = matches.Select(m => m.Id).ToList();
        var now = ScoreCastDateTime.Now.Value;

        // Predictions visible once match has started (kickoff passed)
        var startedMatchIds = matches
            .Where(m => m.KickoffTime.HasValue && m.KickoffTime.Value <= now
                        || m.Status == MatchStatus.Finished)
            .Select(m => m.Id)
            .ToHashSet();

        var allStarted = matchIds.All(id => startedMatchIds.Contains(id));
        var anyStarted = startedMatchIds.Count > 0;

        // Risk plays visible only when ALL matches in GW have started (last match kicked off)
        var riskPlaysVisible = allStarted;
        var predictionsVisible = anyStarted;

        List<MyPredictionResult> predictions = [];
        List<RiskPlayResult> riskPlays = [];

        if (predictionsVisible)
        {
            // Only return predictions for started matches
            predictions = await DbContext.Predictions.AsNoTracking()
                .Where(p => p.SeasonId == query.SeasonId
                            && p.UserId == query.TargetUserId
                            && p.MatchId != null
                            && startedMatchIds.Contains(p.MatchId.Value))
                .Select(p => new MyPredictionResult(p.MatchId!.Value, p.PredictedHomeScore!.Value, p.PredictedAwayScore!.Value, p.Outcome))
                .ToListAsync(ct);
        }

        if (riskPlaysVisible)
        {
            riskPlays = await DbContext.RiskPlays.AsNoTracking()
                .Where(r => r.SeasonId == query.SeasonId && r.GameweekId == query.GameweekId
                            && r.UserId == query.TargetUserId && !r.IsDeleted)
                .Select(r => new RiskPlayResult(r.Id, r.MatchId, r.RiskType, r.Selection,
                    r.BonusPoints, r.IsResolved, r.IsWon))
                .ToListAsync(ct);
        }

        return ScoreCastResponse<PlayerGameweekResult>.Ok(
            new PlayerGameweekResult(predictions, riskPlays, predictionsVisible, riskPlaysVisible));
    }
}
