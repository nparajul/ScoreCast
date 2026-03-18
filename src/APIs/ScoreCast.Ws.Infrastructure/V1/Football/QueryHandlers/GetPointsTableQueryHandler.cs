using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Ws.Application.V1.Football.Queries;
using ScoreCast.Shared.Enums;
using ScoreCast.Ws.Application.V1.Interfaces;

namespace ScoreCast.Ws.Infrastructure.V1.Football.QueryHandlers;

internal sealed record GetPointsTableQueryHandler(
    IScoreCastDbContext DbContext) : IQueryHandler<GetPointsTableQuery, ScoreCastResponse<PointsTableResult>>
{
    public async Task<ScoreCastResponse<PointsTableResult>> ExecuteAsync(GetPointsTableQuery query, CancellationToken ct)
    {
        var season = await DbContext.Seasons
            .AsNoTracking()
            .Include(s => s.Competition)
            .FirstOrDefaultAsync(s => s.Id == query.SeasonId, ct);

        if (season is null)
            return ScoreCastResponse<PointsTableResult>.Error("Season not found");

        var format = season.Competition.Format;

        var matchQuery = DbContext.Matches
            .AsNoTracking()
            .Where(m => m.Gameweek.SeasonId == query.SeasonId);

        // For GroupAndKnockout, include ALL group stage matches (for team/group structure) but only compute stats from finished ones
        if (format == CompetitionFormat.GroupAndKnockout)
            matchQuery = matchQuery.Where(m => m.MatchGroupId != null);
        else
            matchQuery = matchQuery.Where(m => m.Status == MatchStatus.Finished);

        var matches = await matchQuery
            .Select(m => new
            {
                m.Id, m.HomeTeamId, m.AwayTeamId, m.Status,
                HomeScore = m.HomeScore ?? 0, AwayScore = m.AwayScore ?? 0,
                HomeTeamName = m.HomeTeam.Name, HomeTeamLogo = m.HomeTeam.LogoUrl,
                AwayTeamName = m.AwayTeam.Name, AwayTeamLogo = m.AwayTeam.LogoUrl,
                HomeTeamShortName = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                AwayTeamShortName = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                m.KickoffTime,
                m.MatchGroupId,
                MatchGroupName = m.MatchGroup != null ? m.MatchGroup.Name : null,
                MatchGroupOrder = m.MatchGroup != null ? m.MatchGroup.SortOrder : 0
            })
            .ToListAsync(ct);

        var matchIds = matches.Select(m => m.Id).ToList();
        var matchTeams = matches.ToDictionary(m => m.Id, m => (m.HomeTeamId, m.AwayTeamId));

        var goalEvents = await DbContext.MatchEvents
            .AsNoTracking()
            .Where(e => matchIds.Contains(e.MatchId) && (e.EventType == MatchEventType.Goal || e.EventType == MatchEventType.PenaltyGoal || e.EventType == MatchEventType.OwnGoal))
            .Select(e => new { e.MatchId, e.PlayerId, e.Player.Name, e.Value, IsOwnGoal = e.EventType == MatchEventType.OwnGoal })
            .ToListAsync(ct);

        var playerIds = goalEvents.Select(e => e.PlayerId).Distinct().ToList();
        var playerTeamMap = await DbContext.TeamPlayers
            .Where(tp => tp.SeasonId == query.SeasonId && playerIds.Contains(tp.PlayerId))
            .Select(tp => new { tp.PlayerId, tp.TeamId })
            .ToDictionaryAsync(tp => tp.PlayerId, tp => tp.TeamId, ct);

        var goalsByMatch = goalEvents
            .GroupBy(e => e.MatchId)
            .ToDictionary(g => g.Key, g => g.Select(e =>
            {
                var playerTeamId = playerTeamMap.GetValueOrDefault(e.PlayerId);
                var (homeTeamId, _) = matchTeams[e.MatchId];
                var isHome = e.IsOwnGoal ? playerTeamId != homeTeamId : playerTeamId == homeTeamId;
                return new FormGoal(e.Name, e.Value, e.IsOwnGoal, isHome);
            }).ToList());

        // Group matches: by MatchGroup for GroupAndKnockout, single group for League
        var matchGroups = format == CompetitionFormat.GroupAndKnockout
            ? matches.GroupBy(m => (m.MatchGroupId, m.MatchGroupName, m.MatchGroupOrder))
                .OrderBy(g => g.Key.MatchGroupOrder)
                .Select(g => (GroupName: g.Key.MatchGroupName, Matches: g.ToList()))
                .ToList()
            : [(GroupName: (string?)null, Matches: matches)];

        var groups = new List<PointsTableGroup>();
        foreach (var (groupName, groupMatches) in matchGroups)
        {
            var teams = new Dictionary<long, TableEntry>();
            var teamResults = new Dictionary<long, List<(DateTime? Kickoff, string Result, string Opponent, int HomeScore, int AwayScore, bool IsHome, long MatchId)>>();

            foreach (var m in groupMatches)
            {
                var home = GetOrAdd(teams, m.HomeTeamId, m.HomeTeamName, m.HomeTeamShortName, m.HomeTeamLogo);
                var away = GetOrAdd(teams, m.AwayTeamId, m.AwayTeamName, m.AwayTeamShortName, m.AwayTeamLogo);

                if (m.Status != MatchStatus.Finished) continue;

                home.Played++; away.Played++;
                home.GoalsFor += m.HomeScore; home.GoalsAgainst += m.AwayScore;
                away.GoalsFor += m.AwayScore; away.GoalsAgainst += m.HomeScore;

                if (m.HomeScore > m.AwayScore)
                {
                    home.Won++; away.Lost++;
                    AddResult(teamResults, m.HomeTeamId, m.KickoffTime, MatchResultCodes.Win, m.AwayTeamShortName, m.HomeScore, m.AwayScore, true, m.Id);
                    AddResult(teamResults, m.AwayTeamId, m.KickoffTime, MatchResultCodes.Loss, m.HomeTeamShortName, m.HomeScore, m.AwayScore, false, m.Id);
                }
                else if (m.HomeScore < m.AwayScore)
                {
                    away.Won++; home.Lost++;
                    AddResult(teamResults, m.HomeTeamId, m.KickoffTime, MatchResultCodes.Loss, m.AwayTeamShortName, m.HomeScore, m.AwayScore, true, m.Id);
                    AddResult(teamResults, m.AwayTeamId, m.KickoffTime, MatchResultCodes.Win, m.HomeTeamShortName, m.HomeScore, m.AwayScore, false, m.Id);
                }
                else
                {
                    home.Drawn++; away.Drawn++;
                    AddResult(teamResults, m.HomeTeamId, m.KickoffTime, MatchResultCodes.Draw, m.AwayTeamShortName, m.HomeScore, m.AwayScore, true, m.Id);
                    AddResult(teamResults, m.AwayTeamId, m.KickoffTime, MatchResultCodes.Draw, m.HomeTeamShortName, m.HomeScore, m.AwayScore, false, m.Id);
                }
            }

            var rows = teams.Values
                .OrderByDescending(t => t.Points)
                .ThenByDescending(t => t.GoalDifference)
                .ThenByDescending(t => t.GoalsFor)
                .ThenBy(t => t.Name)
                .Select((t, i) => new PointsTableRow(
                    i + 1, t.TeamId, t.Name, t.ShortName, t.Logo,
                    t.Played, t.Won, t.Drawn, t.Lost,
                    t.GoalsFor, t.GoalsAgainst, t.GoalDifference, t.Points,
                    GetForm(teamResults, t.TeamId, goalsByMatch)))
                .ToList();

            groups.Add(new PointsTableGroup(groupName, rows));
        }

        // Best 3rd placed teams (only for GroupAndKnockout)
        var bestThirdPlaced = new List<PointsTableRow>();
        if (format == CompetitionFormat.GroupAndKnockout)
        {
            bestThirdPlaced = groups
                .Where(g => g.Rows.Count >= 3)
                .Select(g => g.Rows[2]) // 3rd placed team from each group
                .OrderByDescending(r => r.Points)
                .ThenByDescending(r => r.GoalDifference)
                .ThenByDescending(r => r.GoalsFor)
                .ToList();
        }

        // Knockout rounds (only for GroupAndKnockout)
        var knockoutRounds = new List<KnockoutRound>();
        if (format == CompetitionFormat.GroupAndKnockout)
        {
            var knockoutMatches = await DbContext.Matches
                .AsNoTracking()
                .Where(m => m.Gameweek.SeasonId == query.SeasonId
                    && m.Gameweek.Stage != null
                    && m.Gameweek.Stage.StageType == StageType.Knockout)
                .Select(m => new
                {
                    m.Id, m.HomeScore, m.AwayScore, m.Status, m.KickoffTime,
                    HomeTeam = m.HomeTeam.ShortName ?? m.HomeTeam.Name,
                    HomeTeamLogo = m.HomeTeam.LogoUrl,
                    AwayTeam = m.AwayTeam.ShortName ?? m.AwayTeam.Name,
                    AwayTeamLogo = m.AwayTeam.LogoUrl,
                    StageName = m.Gameweek.Stage!.Name,
                    StageSortOrder = m.Gameweek.Stage!.SortOrder
                })
                .OrderBy(m => m.StageSortOrder)
                .ThenBy(m => m.KickoffTime)
                .ToListAsync(ct);

            knockoutRounds = knockoutMatches
                .GroupBy(m => (m.StageName, m.StageSortOrder))
                .OrderBy(g => g.Key.StageSortOrder)
                .Select(g => new KnockoutRound(
                    g.Key.StageName,
                    g.Key.StageSortOrder,
                    g.Select(m => new KnockoutMatch(
                        m.Id, m.HomeTeam, m.HomeTeamLogo, m.AwayTeam, m.AwayTeamLogo,
                        m.HomeScore, m.AwayScore, m.Status.ToString(), m.KickoffTime)).ToList()))
                .ToList();
        }

        return ScoreCastResponse<PointsTableResult>.Ok(new PointsTableResult(format, groups, bestThirdPlaced, knockoutRounds));
    }

    private static TableEntry GetOrAdd(Dictionary<long, TableEntry> teams, long id, string name, string? shortName, string? logo)
    {
        if (!teams.TryGetValue(id, out var entry))
        {
            entry = new TableEntry(id, name, shortName, logo);
            teams[id] = entry;
        }
        return entry;
    }

    private static void AddResult(Dictionary<long, List<(DateTime? Kickoff, string Result, string Opponent, int HomeScore, int AwayScore, bool IsHome, long MatchId)>> results,
        long teamId, DateTime? kickoff, string result, string opponent, int homeScore, int awayScore, bool isHome, long matchId)
    {
        if (!results.TryGetValue(teamId, out var list))
        {
            list = [];
            results[teamId] = list;
        }
        list.Add((kickoff, result, opponent, homeScore, awayScore, isHome, matchId));
    }

    private static List<RecentForm> GetForm(Dictionary<long, List<(DateTime? Kickoff, string Result, string Opponent, int HomeScore, int AwayScore, bool IsHome, long MatchId)>> results,
        long teamId, Dictionary<long, List<FormGoal>> goalsByMatch) =>
        results.TryGetValue(teamId, out var list)
            ? list.OrderByDescending(r => r.Kickoff).Take(5).OrderBy(r => r.Kickoff)
                .Select(r => new RecentForm(r.Result, r.Opponent, r.HomeScore, r.AwayScore, r.IsHome,
                    goalsByMatch.GetValueOrDefault(r.MatchId, [])))
                .ToList()
            : [];

    private sealed class TableEntry(long teamId, string name, string? shortName, string? logo)
    {
        public long TeamId => teamId;
        public string Name => name;
        public string? ShortName => shortName;
        public string? Logo => logo;
        public int Played, Won, Drawn, Lost, GoalsFor, GoalsAgainst;
        public int GoalDifference => GoalsFor - GoalsAgainst;
        public int Points => Won * 3 + Drawn;
    }
}
