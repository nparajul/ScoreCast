using Microsoft.JSInterop;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class TeamDetail
{
    [Parameter] public long TeamId { get; set; }

    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private static readonly string[] _tabs = ["Overview", "Fixtures", "Table", "Stats", "Squad"];
    private string _activeTab = "Overview";
    private int _tabIndex;

    private readonly HashSet<long> _expandedMatches = [];

    private TeamDetailResult? _team;
    private TeamMatchesResult? _matches;
    private PointsTableResult? _tableResult;
    private List<PlayerStatRow> _statsRows = [];
    private TeamSquadResult? _squad;

    private long? _tableSeasonId;
    private long? _statsSeasonId;
    private string? _scrollToAnchor;

    private long _loadedTeamId;

    protected override async Task OnInitializedAsync()
    {
        await LoadTeamAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        if (TeamId != _loadedTeamId)
        {
            _team = null;
            _matches = null;
            _tableResult = null;
            _statsRows = [];
            _squad = null;
            _activeTab = _tabs[0];
            _tabIndex = 0;
            _expandedMatches.Clear();
            await LoadTeamAsync();
        }
    }

    private async Task LoadTeamAsync()
    {
        _loadedTeamId = TeamId;
        await Loading.While(async () =>
        {
            var response = await Api.GetTeamDetailAsync(TeamId, CancellationToken.None);
            if (response is { Success: true, Data: not null })
            {
                _team = response.Data;
                var plSeason = _team.Competitions.FirstOrDefault(c => c.CompetitionCode == "PL")
                    ?? _team.Competitions.FirstOrDefault();
                _tableSeasonId = plSeason?.SeasonId;
            }
            else
                Alert.Add(response.Message ?? "Failed to load team", Severity.Error);
        });
    }

    private async Task OnTabChanged(string tab)
    {
        _activeTab = tab;
        _tabIndex = Array.IndexOf(_tabs, tab);
        await LoadTabDataAsync();
        StateHasChanged();
    }

    private async Task LoadTabDataAsync()
    {
        switch (_activeTab)
        {
            case "Fixtures" when _matches is null:
                await Loading.While(async () =>
                {
                    var response = await Api.GetTeamMatchesAsync(TeamId, null, CancellationToken.None);
                    if (response is { Success: true, Data: not null })
                        _matches = response.Data;
                    else
                        Alert.Add(response.Message ?? "Failed to load fixtures", Severity.Error);
                });
                QueueScrollToFocusGroup();
                StateHasChanged();
                break;

            case "Table" when _tableResult is null && _tableSeasonId.HasValue:
                await LoadTableAsync();
                StateHasChanged();
                break;

            case "Stats" when _statsRows.Count == 0:
                await LoadStatsAsync();
                StateHasChanged();
                break;

            case "Squad" when _squad is null:
                await Loading.While(async () =>
                {
                    var response = await Api.GetTeamSquadAsync(TeamId, null, CancellationToken.None);
                    if (response is { Success: true, Data: not null })
                        _squad = response.Data;
                    else
                        Alert.Add(response.Message ?? "Failed to load squad", Severity.Error);
                });
                StateHasChanged();
                break;
        }
    }

    private async Task LoadTableAsync()
    {
        if (!_tableSeasonId.HasValue) return;
        await Loading.While(async () =>
        {
            var response = await Api.GetPointsTableAsync(_tableSeasonId.Value, CancellationToken.None);
            if (response is { Success: true, Data: not null })
                _tableResult = response.Data;
            else
                Alert.Add(response.Message ?? "Failed to load table", Severity.Error);
        });
    }

    private async Task LoadStatsAsync()
    {
        await Loading.While(async () =>
        {
            var response = await Api.GetTeamPlayerStatsAsync(TeamId, _statsSeasonId, CancellationToken.None);
            if (response is { Success: true, Data: not null })
                _statsRows = response.Data.Rows;
            else
                Alert.Add(response.Message ?? "Failed to load stats", Severity.Error);
        });
    }

    // Watch tab index changes from MudTabs (desktop)
    private int TabIndex
    {
        get => _tabIndex;
        set
        {
            _tabIndex = value;
            _activeTab = _tabs[value];
            _ = LoadTabDataAsync();
        }
    }

    // ── Fixtures date grouping (same pattern as Scores page) ──

    private record DateGroup(string Label, string AnchorId, List<TeamMatchDetail> Matches);

    private List<DateGroup> GetMatchesByDate()
    {
        if (_matches is null) return [];
        var today = ScoreCastDateTime.Now.Date;
        return _matches.Matches
            .GroupBy(m => m.KickoffTime is not null ? DateOnly.FromDateTime(m.KickoffTime.Value.ToLocalTime()) : (DateOnly?)null)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var label = g.Key switch
                {
                    null => "TBD",
                    var d when d == today => "Today",
                    var d when d == today.AddDays(1) => "Tomorrow",
                    var d when d == today.AddDays(-1) => "Yesterday",
                    var d => d.Value.ToString("dddd, MMMM d")
                };
                return new DateGroup(label, $"team-date-{g.Key?.ToString("yyyy-MM-dd") ?? "tbd"}", g.ToList());
            })
            .ToList();
    }

    private void QueueScrollToFocusGroup()
    {
        var groups = GetMatchesByDate();
        var today = ScoreCastDateTime.Now.Date;
        var todayAnchor = $"team-date-{today:yyyy-MM-dd}";
        var target = groups.FirstOrDefault(g => g.AnchorId == todayAnchor)
            ?? groups.LastOrDefault(g => g.Matches.Any(m => m.Status == nameof(MatchStatus.Finished)))
            ?? groups.FirstOrDefault();
        _scrollToAnchor = target?.AnchorId;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_scrollToAnchor is not null)
        {
            var anchor = _scrollToAnchor;
            _scrollToAnchor = null;
            await Js.InvokeVoidAsync("eval",
                $"(function(){{var e=document.getElementById('{anchor}');if(e)e.scrollIntoView(true);}})()");
        }
    }

    private void ToggleMatch(long matchId)
    {
        if (!_expandedMatches.Remove(matchId))
            _expandedMatches.Add(matchId);
    }

    private static readonly (string Label, string[] Positions)[] _positionGroups =
    [
        ("GK", [PlayerPositions.Goalkeeper]),
        ("RB", [PlayerPositions.RightBack]),
        ("CB", [PlayerPositions.CentreBack, PlayerPositions.Defence]),
        ("LB", [PlayerPositions.LeftBack]),
        ("DM", [PlayerPositions.DefensiveMidfield]),
        ("CM", [PlayerPositions.CentralMidfield, PlayerPositions.Midfield]),
        ("AM", [PlayerPositions.AttackingMidfield]),
        ("LM", [PlayerPositions.LeftMidfield]),
        ("RM", [PlayerPositions.RightMidfield]),
        ("LW", [PlayerPositions.LeftWinger]),
        ("RW", [PlayerPositions.RightWinger]),
        ("CF", [PlayerPositions.CentreForward, PlayerPositions.Offence]),
    ];

    private static string GetPositionGroup(string? pos)
    {
        if (pos is null) return "Unknown";
        foreach (var g in _positionGroups)
            if (g.Positions.Contains(pos)) return g.Label;
        return "Other";
    }

    private static int GetPositionOrder(string groupLabel)
    {
        for (var i = 0; i < _positionGroups.Length; i++)
            if (_positionGroups[i].Label == groupLabel) return i;
        return 99;
    }
}
