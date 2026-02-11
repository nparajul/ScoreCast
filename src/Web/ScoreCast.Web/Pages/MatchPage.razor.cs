using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class MatchPage : ScoreCastComponentBase, IDisposable
{
    [Parameter] public long MatchId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IClientTimeProvider ClientTime { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private MatchPageResult? _match;
    private bool _loaded;
    private CancellationTokenSource? _pollCts;
    private System.Timers.Timer? _clockTimer;
    private int _elapsedSecs;
    private string? _clockDisplay;
    private string _activeTab = "Events";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await ClientTime.InitializeAsync();
        await Loading.While(LoadAsync);
        _loaded = true;
        StateHasChanged();
        StartPolling();
    }

    private async Task LoadAsync()
    {
        var resp = await Api.GetMatchPageAsync(MatchId, CancellationToken.None);
        if (resp is { Success: true, Data: not null })
        {
            _match = resp.Data;
            InitClock();
        }
    }

    private void InitClock()
    {
        _clockTimer?.Stop();
        _clockTimer?.Dispose();
        _clockTimer = null;

        if (_match is not { Status: nameof(MatchStatus.Live) }) return;

        if (_match.Phase == PulseApi.Phase.HalfTime)
        {
            if (_match.SecondHalfStartMillis is not null)
            {
                var secondHalfStart = DateTimeOffset.FromUnixTimeMilliseconds(_match.SecondHalfStartMillis.Value);
                _htRemainingSeconds = (int)(secondHalfStart - DateTimeOffset.UtcNow).TotalSeconds;
                if (_htRemainingSeconds < 0) _htRemainingSeconds = 0;
                UpdateHtDisplay();
                _clockTimer = new System.Timers.Timer(1000);
                _clockTimer.Elapsed += (_, _) =>
                {
                    _htRemainingSeconds = Math.Max(0, _htRemainingSeconds - 1);
                    UpdateHtDisplay();
                    InvokeAsync(StateHasChanged);
                };
                _clockTimer.Start();
            }
            else
            {
                _clockDisplay = "HT";
            }
            return;
        }

        // Use Pulse clock.secs (actual match time, excludes HT break)
        _elapsedSecs = _match.ClockSeconds ?? 0;

        UpdateClockDisplay();
        _clockTimer = new System.Timers.Timer(1000);
        _clockTimer.Elapsed += (_, _) =>
        {
            _elapsedSecs++;
            UpdateClockDisplay();
            InvokeAsync(StateHasChanged);
        };
        _clockTimer.Start();
    }

    private int _htRemainingSeconds;

    private void UpdateHtDisplay()
    {
        if (_htRemainingSeconds <= 0)
            _clockDisplay = "HT — 2nd half imminent";
        else
        {
            var m = _htRemainingSeconds / 60;
            var s = _htRemainingSeconds % 60;
            _clockDisplay = $"HT — 2nd half in {m}:{s:D2}";
        }
    }

    private void UpdateClockDisplay()
    {
        var mins = _elapsedSecs / 60;
        var secs = _elapsedSecs % 60;
        _clockDisplay = $"{mins}:{secs:D2}";
    }

    private void StartPolling()
    {
        if (_match is not { Status: nameof(MatchStatus.Live) }) return;
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        var token = _pollCts.Token;
        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(token))
            {
                var resp = await Api.GetMatchPageAsync(MatchId, CancellationToken.None);
                if (resp is { Success: true, Data: not null })
                {
                    _match = resp.Data;
                    InitClock();
                    await InvokeAsync(StateHasChanged);
                }
            }
        }, token);
    }

    private string FormatLocal(DateTime utc, string format) => ClientTime.ToLocal(utc).ToString(format);

    private List<MatchPageEvent> OrderedEvents => _match?.Events
        .Where(e => e.EventType is not EventTypes.SubIn and not EventTypes.SubOut)
        .OrderBy(e => _match.Status == nameof(MatchStatus.Live) ? -e.SortKey : e.SortKey)
        .ToList() ?? [];

    private static List<List<MatchPageLineupPlayer>> GetFormationRows(List<MatchPageLineupPlayer> players, string formation, bool reverse)
    {
        if (players.Count == 0) return [];
        var rows = new List<List<MatchPageLineupPlayer>> { new() { players[0] } }; // GK
        var remaining = players.Skip(1).ToList();
        var counts = formation.Split('-').Select(s => int.TryParse(s, out var n) ? n : 0).ToList();
        var idx = 0;
        foreach (var count in counts)
        {
            if (idx + count <= remaining.Count)
                rows.Add(remaining.Skip(idx).Take(count).ToList());
            idx += count;
        }
        if (reverse) rows.Reverse();
        return rows;
    }

    private static string EventIcon(string type) => type switch
    {
        EventTypes.Goal => "⚽",
        EventTypes.PenaltyGoal => "⚽ <span style=\"font-size:10px;\">(P)</span>",
        EventTypes.OwnGoal => "<span style=\"filter:hue-rotate(160deg) saturate(3);\">⚽</span> <span style=\"font-size:10px;color:#d32f2f;\">(OG)</span>",
        EventTypes.YellowCard => "🟨",
        EventTypes.RedCard => "🟥",
        EventTypes.PenaltySaved => "🧤",
        EventTypes.PenaltyMissed => "❌",
        _ => ""
    };

    private static string PlayerIcon(string type) => type switch
    {
        EventTypes.Goal => "⚽",
        EventTypes.PenaltyGoal => "⚽",
        EventTypes.OwnGoal => "⚽(OG)",
        EventTypes.Assist => "👟",
        EventTypes.YellowCard => "🟨",
        EventTypes.RedCard => "🟥",
        _ => ""
    };

    private static string LastName(string name)
    {
        var parts = name.Split(' ');
        return parts.Length > 1 ? parts[^1] : name;
    }

    public void Dispose()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _clockTimer?.Stop();
        _clockTimer?.Dispose();
    }
}
