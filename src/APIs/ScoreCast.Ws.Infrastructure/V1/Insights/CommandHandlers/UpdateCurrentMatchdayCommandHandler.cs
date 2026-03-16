using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application;
using ScoreCast.Ws.Application.V1.Insights.Commands;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Insights.CommandHandlers;

internal sealed record UpdateCurrentMatchdayCommandHandler(
    IScoreCastDbContext DbContext,
    IUnitOfWork UnitOfWork) : ICommandHandler<UpdateCurrentMatchdayCommand, ScoreCastResponse>
{
    public async Task<ScoreCastResponse> ExecuteAsync(UpdateCurrentMatchdayCommand command, CancellationToken ct)
    {
        var seasons = await DbContext.Seasons
            .Where(s => s.IsCurrent && !s.IsDeleted)
            .ToListAsync(ct);

        var updated = 0;
        foreach (var season in seasons)
        {
            // Find the first gameweek that has any non-finished match
            var firstIncomplete = await DbContext.Gameweeks
                .Where(g => g.SeasonId == season.Id && !g.IsDeleted)
                .Where(g => g.Matches.Any(m => m.Status != MatchStatus.Finished && !m.IsDeleted))
                .OrderBy(g => g.Number)
                .Select(g => (int?)g.Number)
                .FirstOrDefaultAsync(ct);

            var newMatchday = firstIncomplete ?? season.CurrentMatchday;
            if (newMatchday is not null && newMatchday != season.CurrentMatchday)
            {
                season.CurrentMatchday = newMatchday;
                updated++;
            }
        }

        if (updated > 0)
            await UnitOfWork.SaveChangesAsync(nameof(UpdateCurrentMatchdayCommand), ct);

        return ScoreCastResponse.Ok($"Updated {updated} season(s)");
    }
}
