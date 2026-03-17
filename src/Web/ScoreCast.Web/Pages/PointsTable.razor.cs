using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class PointsTable : IDisposable
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;

    private PointsTableResult? _result;
    private PointsTableResult? _liveResult;
    private BracketResult? _bracket;
    private List<CompetitionZoneResult> _zones = [];
    private CompetitionResult? _selectedCompetition;
    private SeasonResult? _selectedSeason;
    private string _groupTab = "Groups";
    private bool _hasLive;
    private CancellationTokenSource? _pollCts;

    private PointsTableResult? DisplayResult => _liveResult ?? _result;

    private async Task OnFilterChanged(CompetitionFilterState state)
    {
        _result = null;
        _liveResult = null;
        _bracket = null;
        _zones = [];
        _hasLive = false;
        StopPolling();
        _selectedCompetition = state.Competition;
        _selectedSeason = state.Season;

        if (state.Competition is not null)
        {
            var zonesResponse = await Api.GetCompetitionZonesAsync(state.Competition.Code, CancellationToken.None);
            if (zonesResponse is { Success: true, Data: not null })
                _zones = zonesResponse.Data;
        }

        if (state.Season is not null)
            await LoadTableAsync(state.Season.Id);

        StateHasChanged();
    }

    private async Task LoadTableAsync(long seasonId)
    {
        await Loading.While(async () =>
        {
            var tableTask = Api.GetPointsTableAsync(seasonId, CancellationToken.None);
            var bracketTask = Api.GetBracketAsync(seasonId, CancellationToken.None);
            await Task.WhenAll(tableTask, bracketTask);

            var response = await tableTask;
            if (response is { Success: true, Data: not null })
                _result = response.Data;
            else
                Alert.Add("Failed to load points table", Severity.Error);

            var bracketResponse = await bracketTask;
            if (bracketResponse is { Success: true, Data: not null })
                _bracket = bracketResponse.Data;
        });

        await ApplyLiveOverlay();
        StartPolling();
    }

    private async Task ApplyLiveOverlay()
    {
        if (_result is null || _selectedSeason is null) return;

        var gw = await Api.GetGameweekMatchesAsync(_selectedSeason.Id, SharedConstants.CurrentGameweek, CancellationToken.None);
        if (gw is not { Success: true, Data: not null }) { _liveResult = null; return; }

        var liveMatches = gw.Data.Matches
            .Where(m => m.Status == nameof(MatchStatus.Live))
            .ToList();

        if (liveMatches.Count == 0) { _liveResult = null; _hasLive = false; return; }
        _hasLive = true;

        // Clone groups and apply live scores
        var liveGroups = _result.Groups.Select(g => new PointsTableGroup(
            g.GroupName,
            g.Rows.Select(r => ApplyLiveToRow(r, liveMatches)).ToList()
                .OrderByDescending(r => r.Points)
                .ThenByDescending(r => r.GoalDifference)
                .ThenByDescending(r => r.GoalsFor)
                .Select((r, i) => r with { Position = i + 1 })
                .ToList()
        )).ToList();

        _liveResult = _result with { Groups = liveGroups };
    }

    private static PointsTableRow ApplyLiveToRow(PointsTableRow row, List<MatchDetail> liveMatches)
    {
        var played = row.Played;
        var won = row.Won;
        var drawn = row.Drawn;
        var lost = row.Lost;
        var gf = row.GoalsFor;
        var ga = row.GoalsAgainst;
        var pts = row.Points;

        foreach (var m in liveMatches)
        {
            int? teamGoals = null, oppGoals = null;
            if (m.HomeTeamId == row.TeamId) { teamGoals = m.HomeScore; oppGoals = m.AwayScore; }
            else if (m.AwayTeamId == row.TeamId) { teamGoals = m.AwayScore; oppGoals = m.HomeScore; }
            if (teamGoals is null || oppGoals is null) continue;

            played++;
            gf += teamGoals.Value;
            ga += oppGoals.Value;
            if (teamGoals > oppGoals) { won++; pts += 3; }
            else if (teamGoals == oppGoals) { drawn++; pts += 1; }
            else { lost++; }
        }

        return row with
        {
            Played = played, Won = won, Drawn = drawn, Lost = lost,
            GoalsFor = gf, GoalsAgainst = ga,
            GoalDifference = gf - ga, Points = pts
        };
    }

    private void StartPolling()
    {
        StopPolling();
        _pollCts = new CancellationTokenSource();
        var token = _pollCts.Token;
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(token))
            {
                await ApplyLiveOverlay();
                await InvokeAsync(StateHasChanged);
            }
        }, token);
    }

    private void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;
    }

    private bool IsGroupFormat => _result?.Format is CompetitionFormat.GroupAndKnockout;

    public void Dispose() => StopPolling();
}
