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

        // Compute elapsed from real timestamps, not stale Pulse clock.secs
        var now = DateTimeOffset.UtcNow;
        if (_match.Phase == PulseApi.Phase.SecondHalf && _match.SecondHalfStartMillis is not null)
        {
            // 2nd half: 45:00 + time since 2nd half kickoff
            var shStart = DateTimeOffset.FromUnixTimeMilliseconds(_match.SecondHalfStartMillis.Value);
            _elapsedSecs = 2700 + (int)(now - shStart).TotalSeconds;
        }
        else if (_match.KickoffTime.HasValue)
        {
            // 1st half: time since kickoff
            _elapsedSecs = (int)(now - new DateTimeOffset(_match.KickoffTime.Value, TimeSpan.Zero)).TotalSeconds;
        }
        else
        {
            _elapsedSecs = _match.ClockSeconds ?? 0;
        }

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
        .Where(e => e.EventType is not EventTypes.SubOut)
        .OrderBy(e => _match.Status == nameof(MatchStatus.Live) ? -e.SortKey : e.SortKey)
        .ToList() ?? [];

    private static List<List<MatchPageLineupPlayer>> GetFormationRows(List<MatchPageLineupPlayer> players, string formation, bool reverse, bool mirrorRows = false)
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
        if (mirrorRows)
            foreach (var row in rows) row.Reverse();
        return rows;
    }

    private static string EventIcon(string type) => type switch
    {
        EventTypes.Goal => "⚽",
        EventTypes.PenaltyGoal => "⚽",
        EventTypes.OwnGoal => "<span style=\"filter:hue-rotate(160deg) saturate(3);\">⚽</span>",
        EventTypes.YellowCard => "🟨",
        EventTypes.RedCard => "🟥",
        EventTypes.PenaltySaved => "🧤",
        EventTypes.PenaltyMissed => "❌",
        EventTypes.SubIn => "🔄",
        _ => ""
    };

    private static string FormatRunningScore(string score, bool isHome)
    {
        var parts = score.Split(" - ");
        if (parts.Length != 2) return score;
        return isHome
            ? $"<span style=\"font-weight:800;\">{parts[0]}</span> - {parts[1]}"
            : $"{parts[0]} - <span style=\"font-weight:800;\">{parts[1]}</span>";
    }

    private static string PlayerIcon(string type) => type switch
    {
        EventTypes.Goal => "⚽",
        EventTypes.PenaltyGoal => "⚽",
        EventTypes.OwnGoal => "<span style=\"font-size:9px;\">⚽</span><span style=\"color:#f44336;font-weight:800;font-size:7px;vertical-align:super;\">OG</span>",
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

    private RenderFragment RenderPlayerCard(MatchPageLineupPlayer p, int size) => builder =>
    {
        var s = size;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"text-align:center;width:{s + 16}px;");

        // Avatar container
        builder.OpenElement(5, "div");
        builder.AddAttribute(6, "style", "position:relative;display:inline-block;");

        if (p.PhotoUrl is not null)
        {
            builder.OpenElement(7, "img");
            builder.AddAttribute(8, "src", p.PhotoUrl);
            builder.AddAttribute(9, "style", $"width:{s}px;height:{s}px;border-radius:50%;object-fit:cover;background:#333;border:2px solid rgba(255,255,255,0.3);{(p.SubMinute is not null ? "opacity:0.5;" : "")}");
            builder.CloseElement();
        }
        else
        {
            builder.OpenElement(10, "div");
            builder.AddAttribute(11, "style", $"width:{s}px;height:{s}px;border-radius:50%;background:#555;display:flex;align-items:center;justify-content:center;border:2px solid rgba(255,255,255,0.3);{(p.SubMinute is not null ? "opacity:0.5;" : "")}");
            builder.OpenElement(12, "span");
            builder.AddAttribute(13, "style", $"font-size:{s / 3}px;color:white;font-weight:700;");
            builder.AddContent(14, p.Name.Length > 0 ? p.Name[0].ToString() : "?");
            builder.CloseElement();
            builder.CloseElement();
        }

        // Sub badge
        if (p.SubMinute is not null)
        {
            builder.OpenElement(15, "span");
            builder.AddAttribute(16, "style", "position:absolute;top:-4px;left:-4px;font-size:7px;background:#f44336;color:white;border-radius:6px;padding:1px 3px;font-weight:800;line-height:1;");
            builder.AddContent(17, p.SubMinute);
            builder.CloseElement();
        }
        if (p.IsCaptain)
        {
            builder.OpenElement(18, "span");
            builder.AddAttribute(19, "style", "position:absolute;bottom:-2px;left:-2px;font-size:8px;background:white;color:#333;border-radius:50%;width:14px;height:14px;display:flex;align-items:center;justify-content:center;font-weight:800;");
            builder.AddContent(20, "C");
            builder.CloseElement();
        }
        if (p.Icons.Count > 0)
        {
            var filtered = p.Icons.Where(i => i is not EventTypes.SubIn and not EventTypes.SubOut).ToList();
            if (filtered.Count > 0)
            {
                builder.OpenElement(21, "div");
                builder.AddAttribute(22, "style", "position:absolute;bottom:-2px;right:-4px;font-size:10px;line-height:1;display:flex;gap:1px;");
                foreach (var icon in filtered)
                    builder.AddMarkupContent(23, PlayerIcon(icon));
                builder.CloseElement();
            }
        }

        builder.CloseElement(); // avatar container

        // Shirt number + name
        builder.OpenElement(24, "div");
        builder.AddAttribute(25, "style", $"font-size:{(s < 40 ? 9 : 10)}px;color:white;font-weight:600;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;margin-top:2px;");
        if (p.ShirtNumber is not null)
            builder.AddContent(26, $"{p.ShirtNumber} ");
        builder.AddContent(27, LastName(p.Name));
        builder.CloseElement();

        builder.CloseElement(); // outer div
    };

    public void Dispose()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _clockTimer?.Stop();
        _clockTimer?.Dispose();
    }
}
