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
                AwayTeamName = m.AwayTeam.Name, AwayTeamLogo = m.AwayTeam.LogoUrl,
                HomeTeamShortName = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                AwayTeamShortName = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                m.KickoffTime
            })
            .ToListAsync(ct);

        var teams = new Dictionary<long, TableEntry>();
        var teamResults = new Dictionary<long, List<(DateTime? Kickoff, string Result, string Opponent, int HomeScore, int AwayScore, bool IsHome)>>();

        foreach (var m in matches)
        {
            var home = GetOrAdd(teams, m.HomeTeamId, m.HomeTeamName, m.HomeTeamLogo);
            var away = GetOrAdd(teams, m.AwayTeamId, m.AwayTeamName, m.AwayTeamLogo);

            home.Played++; away.Played++;
            home.GoalsFor += m.HomeScore; home.GoalsAgainst += m.AwayScore;
            away.GoalsFor += m.AwayScore; away.GoalsAgainst += m.HomeScore;

            if (m.HomeScore > m.AwayScore)
            {
                home.Won++; away.Lost++;
                AddResult(teamResults, m.HomeTeamId, m.KickoffTime, "W", m.AwayTeamShortName, m.HomeScore, m.AwayScore, true);
                AddResult(teamResults, m.AwayTeamId, m.KickoffTime, "L", m.HomeTeamShortName, m.HomeScore, m.AwayScore, false);
            }
            else if (m.HomeScore < m.AwayScore)
            {
                away.Won++; home.Lost++;
                AddResult(teamResults, m.HomeTeamId, m.KickoffTime, "L", m.AwayTeamShortName, m.HomeScore, m.AwayScore, true);
                AddResult(teamResults, m.AwayTeamId, m.KickoffTime, "W", m.HomeTeamShortName, m.HomeScore, m.AwayScore, false);
            }
            else
            {
                home.Drawn++; away.Drawn++;
                AddResult(teamResults, m.HomeTeamId, m.KickoffTime, "D", m.AwayTeamShortName, m.HomeScore, m.AwayScore, true);
                AddResult(teamResults, m.AwayTeamId, m.KickoffTime, "D", m.HomeTeamShortName, m.HomeScore, m.AwayScore, false);
            }
        }

        var rows = teams.Values
            .OrderByDescending(t => t.Points)
            .ThenByDescending(t => t.GoalDifference)
            .ThenByDescending(t => t.GoalsFor)
            .ThenBy(t => t.Name)
            .Select((t, i) => new LeagueTableRow(
                i + 1, t.TeamId, t.Name, t.Logo,
                t.Played, t.Won, t.Drawn, t.Lost,
                t.GoalsFor, t.GoalsAgainst, t.GoalDifference, t.Points,
                GetForm(teamResults, t.TeamId)))
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

    private static void AddResult(Dictionary<long, List<(DateTime? Kickoff, string Result, string Opponent, int HomeScore, int AwayScore, bool IsHome)>> results,
        long teamId, DateTime? kickoff, string result, string opponent, int homeScore, int awayScore, bool isHome)
    {
        if (!results.TryGetValue(teamId, out var list))
        {
            list = [];
            results[teamId] = list;
        }
        list.Add((kickoff, result, opponent, homeScore, awayScore, isHome));
    }

    private static List<RecentForm> GetForm(Dictionary<long, List<(DateTime? Kickoff, string Result, string Opponent, int HomeScore, int AwayScore, bool IsHome)>> results, long teamId) =>
        results.TryGetValue(teamId, out var list)
            ? list.OrderByDescending(r => r.Kickoff).Take(5).Select(r => new RecentForm(r.Result, r.Opponent, r.HomeScore, r.AwayScore, r.IsHome)).ToList()
            : [];

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
