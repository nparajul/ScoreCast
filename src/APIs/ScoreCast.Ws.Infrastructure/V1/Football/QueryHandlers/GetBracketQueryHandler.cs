using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Config.Queries;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetBracketQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetBracketQuery, ScoreCastResponse<BracketResult>>
{
    public async Task<ScoreCastResponse<BracketResult>> ExecuteAsync(GetBracketQuery query, CancellationToken ct)
    {
        var season = await DbContext.Seasons
            .AsNoTracking()
            .Include(s => s.Competition)
            .FirstOrDefaultAsync(s => s.Id == query.SeasonId, ct);

        if (season is null)
            return ScoreCastResponse<BracketResult>.Error("Season not found");

        var configKey = $"{AppConfigKeys.BracketTemplatePrefix}{season.Competition.Code}:{season.Name}";
        var templateJson = await new GetAppConfigQuery(configKey).ExecuteAsync(ct);

        if (templateJson is null)
            return ScoreCastResponse<BracketResult>.Ok(new BracketResult([]));

        var templateRounds = JsonSerializer.Deserialize<List<BracketTemplateRound>>(templateJson, _jsonOptions)
            ?? [];

        // Build group standings lookup for resolving labels like 1A, 2B
        var groupStandings = await BuildGroupStandings(query.SeasonId, ct);

        // Fetch real knockout matches
        var knockoutByStage = await FetchKnockoutMatches(query.SeasonId, ct);

        var rounds = new List<BracketRound>();
        foreach (var tmpl in templateRounds)
        {
            var realMatches = knockoutByStage.GetValueOrDefault(tmpl.Name, []);
            var slots = new List<BracketSlot>();

            for (var i = 0; i < tmpl.Slots.Count; i++)
            {
                var s = tmpl.Slots[i];
                var homeResolved = ResolveLabel(s.Home, groupStandings);
                var awayResolved = ResolveLabel(s.Away, groupStandings);

                string? homeTeam = null, awayTeam = null;
                int? homeScore = null, awayScore = null;

                if (i < realMatches.Count && realMatches[i].HomeTeam is not null)
                {
                    homeTeam = realMatches[i].HomeTeam;
                    awayTeam = realMatches[i].AwayTeam;
                    homeScore = realMatches[i].HomeScore;
                    awayScore = realMatches[i].AwayScore;
                }

                slots.Add(new BracketSlot(homeResolved, awayResolved, s.Date, homeTeam, awayTeam, homeScore, awayScore));
            }

            rounds.Add(new BracketRound(tmpl.Name, slots));
        }

        return ScoreCastResponse<BracketResult>.Ok(new BracketResult(rounds));
    }

    private static string ResolveLabel(string label, Dictionary<string, List<string>> groupStandings)
    {
        if (label.Length < 2) return label;
        var pos = label[0] - '0';
        if (pos is not (1 or 2)) return label;
        var groupLetter = label[1..];
        if (groupLetter.Length != 1) return label;
        if (!groupStandings.TryGetValue(groupLetter, out var teams)) return label;
        return pos <= teams.Count ? teams[pos - 1] : label;
    }

    private async Task<Dictionary<string, List<string>>> BuildGroupStandings(long seasonId, CancellationToken ct)
    {
        var matches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == seasonId
                && m.MatchGroupId != null
                && m.Status == MatchStatus.Finished)
            .Select(m => new
            {
                m.HomeTeamId, m.AwayTeamId,
                HomeScore = m.HomeScore ?? 0, AwayScore = m.AwayScore ?? 0,
                HomeTeamName = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                AwayTeamName = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                GroupName = m.MatchGroup!.Name
            })
            .ToListAsync(ct);

        if (matches.Count == 0) return [];

        var result = new Dictionary<string, List<string>>();
        foreach (var group in matches.GroupBy(m => m.GroupName))
        {
            var teams = new Dictionary<long, (string Name, int Pts, int Gd, int Gf)>();
            foreach (var m in group)
            {
                var (hn, hp, hgd, hgf) = teams.GetValueOrDefault(m.HomeTeamId, (m.HomeTeamName, 0, 0, 0));
                var (an, ap, agd, agf) = teams.GetValueOrDefault(m.AwayTeamId, (m.AwayTeamName, 0, 0, 0));

                var hPts = m.HomeScore > m.AwayScore ? 3 : m.HomeScore == m.AwayScore ? 1 : 0;
                var aPts = m.AwayScore > m.HomeScore ? 3 : m.AwayScore == m.HomeScore ? 1 : 0;

                teams[m.HomeTeamId] = (hn, hp + hPts, hgd + m.HomeScore - m.AwayScore, hgf + m.HomeScore);
                teams[m.AwayTeamId] = (an, ap + aPts, agd + m.AwayScore - m.HomeScore, agf + m.AwayScore);
            }

            var groupLetter = group.Key[^1..]; // last char
            result[groupLetter] = teams.Values
                .OrderByDescending(t => t.Pts)
                .ThenByDescending(t => t.Gd)
                .ThenByDescending(t => t.Gf)
                .Select(t => t.Name)
                .ToList();
        }

        return result;
    }

    private async Task<Dictionary<string, List<KnockoutMatchData>>> FetchKnockoutMatches(long seasonId, CancellationToken ct)
    {
        var matches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == seasonId
                && m.Gameweek.Stage != null
                && m.Gameweek.Stage.StageType == StageType.Knockout)
            .Select(m => new
            {
                HomeTeam = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                AwayTeam = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                m.HomeScore, m.AwayScore, m.KickoffTime,
                StageName = m.Gameweek.Stage!.Name,
                StageSortOrder = m.Gameweek.Stage!.SortOrder
            })
            .OrderBy(m => m.StageSortOrder)
            .ThenBy(m => m.KickoffTime)
            .ToListAsync(ct);

        return matches
            .GroupBy(m => m.StageName)
            .ToDictionary(g => g.Key, g => g.Select(m =>
                new KnockoutMatchData(m.HomeTeam, m.AwayTeam, m.HomeScore, m.AwayScore)).ToList());
    }

    private record KnockoutMatchData(string? HomeTeam, string? AwayTeam, int? HomeScore, int? AwayScore);
    private record BracketTemplateRound(string Name, List<BracketTemplateSlot> Slots);
    private record BracketTemplateSlot(string Home, string Away, string? Date);

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
}
