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
            // Active gameweek = the one currently in play, or next upcoming if none active
            var activeGw = await DbContext.Gameweeks
                .Where(g => g.SeasonId == season.Id && g.Status == GameweekStatus.Active && !g.IsDeleted)
                .Select(g => (int?)g.Number)
                .FirstOrDefaultAsync(ct);

            activeGw ??= await DbContext.Gameweeks
                .Where(g => g.SeasonId == season.Id && g.Status != GameweekStatus.Completed && !g.IsDeleted)
                .OrderBy(g => g.Number)
                .Select(g => (int?)g.Number)
                .FirstOrDefaultAsync(ct);

            if (activeGw is not null && activeGw != season.CurrentMatchday)
            {
                season.CurrentMatchday = activeGw;
                updated++;
            }

            // Also advance gameweek statuses: if all matches finished → Completed, if any in progress → Active
            var gameweeks = await DbContext.Gameweeks
                .Include(g => g.Matches)
                .Where(g => g.SeasonId == season.Id && g.Status != GameweekStatus.Completed && !g.IsDeleted)
                .OrderBy(g => g.Number)
                .ToListAsync(ct);

            foreach (var gw in gameweeks)
            {
                var matches = gw.Matches.Where(m => !m.IsDeleted).ToList();
                if (matches.Count == 0) continue;

                if (matches.All(m => m.Status is MatchStatus.Finished or MatchStatus.Postponed))
                    gw.Status = GameweekStatus.Completed;
                else if (matches.Any(m => m.Status is MatchStatus.Live or MatchStatus.Finished))
                    gw.Status = GameweekStatus.Active;
            }
        }

        await UnitOfWork.SaveChangesAsync(command.Request.AppName ?? nameof(UpdateCurrentMatchdayCommand), ct);

        return ScoreCastResponse.Ok($"Updated {updated} season(s)");
    }
}
