using Microsoft.JSInterop;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
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

    private const string AppName = "SCORES";

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

    private const string YellowCardHtml = "<span style=\"display:inline-block;width:8px;height:11px;background:#fdd835;border-radius:1px;vertical-align:middle;\"></span>";
    private const string RedCardHtml = "<span style=\"display:inline-block;width:8px;height:11px;background:#d32f2f;border-radius:1px;vertical-align:middle;\"></span>";

    private const string AssistHtml = "👟";

    private const string PenaltyMissedHtml = "<svg style=\"display:inline-block;vertical-align:middle;\" width=\"14\" height=\"14\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"2\" stroke-linecap=\"round\"><rect x=\"3\" y=\"4\" width=\"18\" height=\"14\" rx=\"0\" fill=\"none\"/><line x1=\"9\" y1=\"8\" x2=\"15\" y2=\"14\"/><line x1=\"15\" y1=\"8\" x2=\"9\" y2=\"14\"/></svg>";

    private static string FormatEvent(MatchEventDetail e) => e.EventType switch
    {
        EventTypes.Goal => e.Value > 1 ? $"⚽ x{e.Value}" : "⚽",
        EventTypes.PenaltyGoal => "⚽ (P)",
        EventTypes.OwnGoal => "⚽ (OG)",
        EventTypes.Assist => AssistHtml,
        EventTypes.YellowCard => YellowCardHtml,
        EventTypes.RedCard => RedCardHtml,
        EventTypes.PenaltySaved => "🧤",
        EventTypes.PenaltyMissed => PenaltyMissedHtml,
        _ => ""
    };

    private record DisplayLine(MarkupString Markup, string? Minute, double SortKey, bool Bold);

    private static List<DisplayLine> GetDisplayLines(List<MatchEventDetail> events, bool isHome, bool includeSubs = true)
    {
        var lines = new List<DisplayLine>();

        foreach (var e in events.Where(e => e.IsHome == isHome && e.EventType is not EventTypes.SubIn and not EventTypes.SubOut))
        {
            var isGoal = e.EventType is EventTypes.Goal or EventTypes.PenaltyGoal or EventTypes.OwnGoal;
            var text = isHome ? $"{e.PlayerName} {FormatEvent(e)}" : $"{FormatEvent(e)} {e.PlayerName}";
            lines.Add(new DisplayLine(
                new MarkupString(text),
                e.Minute, ParseMinute(e.Minute), isGoal));
        }

        if (includeSubs)
        {
            foreach (var s in GetSubPairs(events, isHome))
                lines.Add(new DisplayLine(
                    new MarkupString($"<span style=\"color:#4caf50;\">▲</span> {s.PlayerOn} <span style=\"color:#f44336;\">▼</span> {s.PlayerOff}"),
                    s.Minute, ParseMinute(s.Minute), false));
        }

        return lines.OrderBy(l => l.SortKey).ToList();
    }

    private static double ParseMinute(string? minute)
    {
        if (minute is null) return 999;
        var clean = minute.Replace("'", "").Replace(" ", "");
        var parts = clean.Split('+');
        if (double.TryParse(parts[0], out var main))
            return parts.Length > 1 && double.TryParse(parts[1], out var added) ? main + added * 0.01 : main;
        return 999;
    }

    private record DateGroup(string Label, string AnchorId, List<MatchDetail> Matches);

    private List<DateGroup> GetMatchesByDate()
    {
        if (_gameweek is null) return [];

        var today = DateOnly.FromDateTime(DateTime.Now);
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
                $"(function(){{var c=document.getElementById('scores-scroll-area');var e=document.getElementById('{anchor}');if(c&&e){{var r=e.getBoundingClientRect();var cr=c.getBoundingClientRect();c.scrollTop+=r.top-cr.top;}}}})()");
        }
    }

    private record SubPair(string PlayerOn, string PlayerOff, string? Minute);

    private static List<SubPair> GetSubPairs(List<MatchEventDetail> events, bool isHome)
    {
        var subs = events.Where(e => e.IsHome == isHome).ToList();
        var subIns = subs.Where(e => e.EventType == EventTypes.SubIn).ToList();
        var subOffs = subs.Where(e => e.EventType == EventTypes.SubOut).ToList();

        return subIns.Select(si =>
        {
            var off = subOffs.FirstOrDefault(so => so.Minute == si.Minute);
            if (off is not null) subOffs.Remove(off);
            return new SubPair(si.PlayerName, off?.PlayerName ?? "", si.Minute);
        }).ToList();
    }
}
