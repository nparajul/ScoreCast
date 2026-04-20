using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Interfaces;
using ScoreCast.Ws.Application.V1.PredictionGame.Commands;
using ScoreCast.Ws.Domain.V1.Entities.Football;

namespace ScoreCast.Ws.Infrastructure.V1.PredictionGame.CommandHandlers;

internal sealed record SubmitRiskPlaysCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<SubmitRiskPlaysCommand, ScoreCastResponse>
{
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
            .Select(m => new { m.Id, m.GameweekId, m.Status, m.KickoffTime })
            .ToListAsync(ct);

        if (matches.Count == 0) return ScoreCastResponse.Error("No valid matches");

        var now = ScoreCastDateTime.Now.Value;
        var gameweekId = matches[0].GameweekId;

        // Reject matches that have started (by status OR kickoff time)
        var lockedIds = matches
            .Where(m => m.Status != MatchStatus.Scheduled
                        || (m.KickoffTime.HasValue && m.KickoffTime.Value <= now))
            .Select(m => m.Id).ToHashSet();

        // Filter out risk plays on locked matches
        var validPlays = request.RiskPlays.Where(r => !lockedIds.Contains(r.MatchId)).ToList();
        if (validPlays.Count == 0 && request.RiskPlays.Count > 0)
            return ScoreCastResponse.Error("All selected matches have already kicked off");

        // Load existing risk plays for this GW
        var existing = await DbContext.RiskPlays
            .Where(r => r.UserId == user.Id && r.GameweekId == gameweekId && !r.IsDeleted)
            .ToListAsync(ct);

        foreach (var entry in validPlays)
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

        // Remove risk plays not in the new submission for this GW (only for unlocked matches)
        var submittedKeys = validPlays.Select(r => (r.MatchId, r.RiskType)).ToHashSet();
        foreach (var old in existing.Where(r => !lockedIds.Contains(r.MatchId) && !submittedKeys.Contains((r.MatchId, r.RiskType))))
            old.IsDeleted = true;

        await UnitOfWork.SaveChangesAsync(request.AppName ?? nameof(SubmitRiskPlaysCommand), ct);
        return ScoreCastResponse.Ok($"Saved {validPlays.Count} risk plays");
    }
}
