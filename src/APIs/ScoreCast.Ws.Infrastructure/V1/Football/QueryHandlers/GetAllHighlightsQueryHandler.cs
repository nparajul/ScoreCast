using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetAllHighlightsQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetAllHighlightsQuery, ScoreCastResponse<AllHighlightsResult>>
{
    public async Task<ScoreCastResponse<AllHighlightsResult>> ExecuteAsync(GetAllHighlightsQuery query, CancellationToken ct)
    {
        var items = await DbContext.MatchHighlights.AsNoTracking()
            .Where(h => h.Type == HighlightType.Short)
            .OrderByDescending(h => h.Match.KickoffTime)
            .Skip(query.Skip)
            .Take(query.Take + 1)
            .Select(h => new HighlightItem(
                h.MatchId, h.Match.HomeTeam.Name, h.Match.AwayTeam.Name,
                h.Match.HomeTeam.ShortName, h.Match.AwayTeam.ShortName,
                h.Match.HomeTeam.LogoUrl, h.Match.AwayTeam.LogoUrl,
                h.Match.HomeScore, h.Match.AwayScore, h.Match.KickoffTime,
                h.Match.Gameweek.Season.Competition.Name,
                h.Title, h.EmbedHtml))
            .ToListAsync(ct);

        var hasMore = items.Count > query.Take;
        if (hasMore) items = items.Take(query.Take).ToList();

        return ScoreCastResponse<AllHighlightsResult>.Ok(new AllHighlightsResult(items, hasMore));
    }
}
