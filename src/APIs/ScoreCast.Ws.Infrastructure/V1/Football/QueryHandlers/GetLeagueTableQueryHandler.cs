using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Ws.Application.Interfaces;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Ws.Domain.V1.Enums;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetLeagueTableQueryHandler(
    IScoreCastDbContext DbContext) : ICommandHandler<GetLeagueTableQuery, ScoreCastResponse<List<LeagueTableRow>>>
{
    public async Task<ScoreCastResponse<List<LeagueTableRow>>> ExecuteAsync(GetLeagueTableQuery query, CancellationToken ct)
    {
        var matches = await DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == query.SeasonId && m.Status == MatchStatus.Finished)
            .Select(m => new
            {
                m.HomeTeamId, m.AwayTeamId,
                HomeScore = m.HomeScore ?? 0, AwayScore = m.AwayScore ?? 0,
                HomeTeamName = m.HomeTeam.Name, HomeTeamLogo = m.HomeTeam.LogoUrl,
                AwayTeamName = m.AwayTeam.Name, AwayTeamLogo = m.AwayTeam.LogoUrl
            })
            .ToListAsync(ct);

        var teams = new Dictionary<long, TableEntry>();

        foreach (var m in matches)
        {
            var home = GetOrAdd(teams, m.HomeTeamId, m.HomeTeamName, m.HomeTeamLogo);
            var away = GetOrAdd(teams, m.AwayTeamId, m.AwayTeamName, m.AwayTeamLogo);

            home.Played++; away.Played++;
            home.GoalsFor += m.HomeScore; home.GoalsAgainst += m.AwayScore;
            away.GoalsFor += m.AwayScore; away.GoalsAgainst += m.HomeScore;

            if (m.HomeScore > m.AwayScore) { home.Won++; away.Lost++; }
            else if (m.HomeScore < m.AwayScore) { away.Won++; home.Lost++; }
            else { home.Drawn++; away.Drawn++; }
        }

        var rows = teams.Values
            .OrderByDescending(t => t.Points)
            .ThenByDescending(t => t.GoalDifference)
            .ThenByDescending(t => t.GoalsFor)
            .ThenBy(t => t.Name)
            .Select((t, i) => new LeagueTableRow(
                i + 1, t.TeamId, t.Name, t.Logo,
                t.Played, t.Won, t.Drawn, t.Lost,
                t.GoalsFor, t.GoalsAgainst, t.GoalDifference, t.Points))
            .ToList();

        return ScoreCastResponse<List<LeagueTableRow>>.Ok(rows);
    }

    private static TableEntry GetOrAdd(Dictionary<long, TableEntry> teams, long id, string name, string? logo)
    {
        if (!teams.TryGetValue(id, out var entry))
        {
            entry = new TableEntry(id, name, logo);
            teams[id] = entry;
        }
        return entry;
    }

    private sealed class TableEntry(long teamId, string name, string? logo)
    {
        public long TeamId => teamId;
        public string Name => name;
        public string? Logo => logo;
        public int Played, Won, Drawn, Lost, GoalsFor, GoalsAgainst;
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public int Points => Won * 3 + Drawn;
    }
}
