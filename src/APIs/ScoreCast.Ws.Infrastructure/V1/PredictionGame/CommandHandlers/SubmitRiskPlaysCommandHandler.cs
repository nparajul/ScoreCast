using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.CommandHandlers;

internal sealed record SubmitRiskPlaysCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<SubmitRiskPlaysCommand, ScoreCastResponse>
{
    private const int MaxDoubleDown = 1;
    private const int MaxExactScoreBoost = 1;
    private const int MaxMinorRiskPlays = 2; // CleanSheet + FirstGoal + OverUnder combined

    public async Task<ScoreCastResponse> ExecuteAsync(SubmitRiskPlaysCommand command, CancellationToken ct)
    {
        var request = command.Request;

        var user = await DbContext.UserMasters
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, ct);
        if (user is null) return ScoreCastResponse.Error("User not found");

        // Get gameweek from first match
        var matchIds = request.RiskPlays.Select(r => r.MatchId).Distinct().ToList();
        var matches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => matchIds.Contains(m.Id))
            .Select(m => new { m.Id, m.GameweekId, m.Status })
            .ToListAsync(ct);

        if (matches.Count == 0) return ScoreCastResponse.Error("No valid matches");

        var gameweekId = matches[0].GameweekId;

        // Validate no locked matches
        var lockedIds = matches.Where(m => m.Status != MatchStatus.Scheduled).Select(m => m.Id).ToHashSet();
        if (request.RiskPlays.Any(r => lockedIds.Contains(r.MatchId)))
            return ScoreCastResponse.Error("Cannot place risk plays on started/finished matches");

        // Validate limits
        var ddCount = request.RiskPlays.Count(r => r.RiskType == RiskPlayType.DoubleDown);
        var esCount = request.RiskPlays.Count(r => r.RiskType == RiskPlayType.ExactScoreBoost);
        var minorCount = request.RiskPlays.Count(r => r.RiskType is RiskPlayType.CleanSheetBet or RiskPlayType.FirstGoalTeam or RiskPlayType.OverUnderGoals);

        if (ddCount > MaxDoubleDown) return ScoreCastResponse.Error("Max 1 Double Down per gameweek");
        if (esCount > MaxExactScoreBoost) return ScoreCastResponse.Error("Max 1 Exact Score Boost per gameweek");
        if (minorCount > MaxMinorRiskPlays) return ScoreCastResponse.Error("Max 2 minor risk plays per gameweek");

        // Load existing risk plays for this GW
        var existing = await DbContext.RiskPlays
            .Where(r => r.UserId == user.Id && r.GameweekId == gameweekId && !r.IsDeleted)
            .ToListAsync(ct);

        foreach (var entry in request.RiskPlays)
        {
            var ex = existing.FirstOrDefault(r => r.MatchId == entry.MatchId && r.RiskType == entry.RiskType);
            if (ex is not null)
            {
                ex.Selection = entry.Selection;
            }
            else
            {
                DbContext.RiskPlays.Add(new RiskPlay
                {
                    SeasonId = request.SeasonId,
                    GameweekId = gameweekId,
                    UserId = user.Id,
                    MatchId = entry.MatchId,
                    RiskType = entry.RiskType,
                    Selection = entry.Selection
                });
            }
        }

        // Remove risk plays not in the new submission for this GW
        var submittedKeys = request.RiskPlays.Select(r => (r.MatchId, r.RiskType)).ToHashSet();
        foreach (var old in existing.Where(r => !submittedKeys.Contains((r.MatchId, r.RiskType))))
            old.IsDeleted = true;

        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(SubmitRiskPlaysCommand), ct);
        return ScoreCastResponse.Ok($"Saved {request.RiskPlays.Count} risk plays");
    }
}
