using Microsoft.JSInterop;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Scores : IDisposable
{
    private CancellationTokenSource? _pollCts;
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private const string _appName = "SCORES";

    private SeasonResult? _selectedSeason;
    private GameweekMatchesResult? _gameweek;
    private readonly HashSet<long> _expandedMatches = [];

    private async Task OnFilterChanged(CompetitionFilterState state)
    {
        _selectedSeason = state.Season;
        _gameweek = null;
        _expandedMatches.Clear();
        if (state.Season is not null)
            await LoadGameweekAsync(state.Season.Id, SharedConstants.CurrentGameweek);
        StateHasChanged();
    }

    private string? _scrollToAnchor;

    private async Task LoadGameweekAsync(long seasonId, int gameweekNumber)
    {
        _expandedMatches.Clear();
        await Loading.While(async () =>
        {
            var response = await Api.GetGameweekMatchesAsync(seasonId, gameweekNumber, CancellationToken.None);
            if (response is { Success: true, Data: not null })
                _gameweek = response.Data;
            else
                Alert.Add("Failed to load matches", Severity.Error);
        });
        StartOrStopPolling();
        QueueScrollToFocusGroup();
    }

    private void StartOrStopPolling()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _pollCts = null;

        if (_gameweek?.Matches.Any(m => m.Status == nameof(MatchStatus.Live)) is not true || _selectedSeason is null) return;

        _pollCts = new CancellationTokenSource();
        var token = _pollCts.Token;
        var seasonId = _selectedSeason.Id;
        var gameweekNumber = _gameweek.GameweekNumber;

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(token))
            {
                var response = await Api.GetGameweekMatchesAsync(seasonId, gameweekNumber, CancellationToken.None);
                if (response is { Success: true, Data: not null })
                {
                    _gameweek = response.Data;
                    await InvokeAsync(StateHasChanged);
                    if (!_gameweek.Matches.Any(m => m.Status == nameof(MatchStatus.Live)))
                        break;
                }
            }
        }, token);
    }

    public void Dispose()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
    }

    private async Task PreviousGameweek()
    {
        if (_selectedSeason is not null && _gameweek is not null && _gameweek.GameweekNumber > 1)
            await LoadGameweekAsync(_selectedSeason.Id, _gameweek.GameweekNumber - 1);
    }

    private async Task NextGameweek()
    {
        if (_selectedSeason is not null && _gameweek is not null && _gameweek.GameweekNumber < _gameweek.TotalGameweeks)
            await LoadGameweekAsync(_selectedSeason.Id, _gameweek.GameweekNumber + 1);
    }

    private void ToggleMatch(long matchId)
    {
        if (!_expandedMatches.Remove(matchId))
            _expandedMatches.Add(matchId);
    }

    private record DateGroup(string Label, string AnchorId, List<MatchDetail> Matches);

    private List<DateGroup> GetMatchesByDate()
    {
        if (_gameweek is null) return [];

        var today = ScoreCastDateTime.Now.Date;
        return _gameweek.Matches
            .GroupBy(m => m.KickoffTime.HasValue ? DateOnly.FromDateTime(m.KickoffTime.Value.ToLocalTime()) : (DateOnly?)null)
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
                var anchorId = $"date-{g.Key?.ToString("yyyy-MM-dd") ?? "tbd"}";
                return new DateGroup(label, anchorId, g.ToList());
            })
            .ToList();
    }

    private void QueueScrollToFocusGroup()
    {
        if (_gameweek is null) return;
        var groups = GetMatchesByDate();
        var target = groups.FirstOrDefault(g => g.Label == "Today") ?? groups.FirstOrDefault();
        _scrollToAnchor = target?.AnchorId;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_scrollToAnchor is not null)
        {
            var anchor = _scrollToAnchor;
            _scrollToAnchor = null;
            await Js.InvokeVoidAsync("eval",
                $"document.getElementById('{anchor}')?.scrollIntoView({{behavior:'smooth',block:'start'}})");
        }
    }
}
