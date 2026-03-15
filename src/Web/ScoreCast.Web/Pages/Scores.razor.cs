using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Scores : IDisposable
{
    private CancellationTokenSource? _pollCts;
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;

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

    private static string FormatEvent(MatchEventDetail e) => e.EventType switch
    {
        EventTypes.Goal => e.Value > 1 ? $"⚽ x{e.Value}" : "⚽",
        EventTypes.PenaltyGoal => "⚽ (P)",
        EventTypes.OwnGoal => "⚽ (OG)",
        EventTypes.Assist => "🅰️",
        EventTypes.YellowCard => "🟨",
        EventTypes.RedCard => "🟥",
        EventTypes.PenaltySaved => "🧤",
        EventTypes.PenaltyMissed => "❌",
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
