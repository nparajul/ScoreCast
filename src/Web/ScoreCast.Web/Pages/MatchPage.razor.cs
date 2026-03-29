using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;
using ScoreCast.Web.Components.Shared;

namespace ScoreCast.Web.Pages;

public partial class MatchPage : ScoreCastComponentBase, IDisposable
{
    [Parameter] public long MatchId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IClientTimeProvider ClientTime { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    protected override string PageKey => $"match-{MatchId}";

    private MatchPageResult? _match;
    private MatchExtrasResult? _extras;
    private PredictionReplayResult? _replay;
    private MatchHighlightsResult? _highlights;
    private bool _highlightsLoading;
    private bool _loaded;
    private CancellationTokenSource? _pollCts;
    private System.Timers.Timer? _clockTimer;
    private string? _clockDisplay;
    private string _activeTab = "Events";
    private string _lineupTab = "home";
    private string? _playingHighlight;
    private PointsTableResult? _table;
    private List<CompetitionZoneResult> _zones = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _activeTab = RestoreState("tab", "Events")!;
        await ClientTime.InitializeAsync();
        await Loading.While(LoadAsync);
        _loaded = true;
        StateHasChanged();
        await RestoreScrollAsync();
        StartPolling();
    }

    private MatchPredictionResult? _prediction;

    private async Task LoadAsync()
    {
        var resp = await Api.GetMatchPageAsync(MatchId, CancellationToken.None);
        if (resp is { Success: true, Data: not null })
        {
            _match = resp.Data;
            InitClock();
            _ = LoadTableAsync();
            if (_match.Status == nameof(MatchStatus.Scheduled))
                _ = LoadPredictionAsync();
        }
    }

    private async Task LoadPredictionAsync()
    {
        var resp = await Api.GetMatchPredictionAsync(MatchId, CancellationToken.None);
        if (resp is { Success: true, Data: not null })
            _prediction = resp.Data;
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadTableAsync()
    {
        var tableTask = Api.GetPointsTableAsync(_match!.SeasonId, CancellationToken.None);
        var zonesTask = Api.GetCompetitionZonesAsync(_match.CompetitionCode, CancellationToken.None);
        await Task.WhenAll(tableTask, zonesTask);
        if ((await tableTask) is { Success: true, Data: not null } t) _table = t.Data;
        if ((await zonesTask) is { Success: true, Data: not null } z) _zones = z.Data;
        await InvokeAsync(StateHasChanged);
    }

    private void InitClock()
    {
        _clockTimer?.Stop();
        _clockTimer?.Dispose();
        _clockTimer = null;

        if (_match is not { Status: nameof(MatchStatus.Live) }) return;

        if (_match.Phase == PulseApi.Phase.HalfTime)
        {
            _clockDisplay = "HT";
            return;
        }

        UpdateClockDisplay();
        _clockTimer = new System.Timers.Timer(1000);
        _clockTimer.Elapsed += (_, _) => { UpdateClockDisplay(); InvokeAsync(StateHasChanged); };
        _clockTimer.Start();
    }

    private void UpdateClockDisplay()
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int totalSecs;

        if (_match?.Phase == PulseApi.Phase.SecondHalf && _match.SecondHalfStartMillis is not null)
            totalSecs = (int)((now - _match.SecondHalfStartMillis.Value) / 1000) + 2700; // +45:00
        else if (_match?.FirstHalfStartMillis is not null)
            totalSecs = (int)((now - _match.FirstHalfStartMillis.Value) / 1000);
        else
            return;

        if (totalSecs < 0) totalSecs = 0;
        _clockDisplay = $"{totalSecs / 60}:{totalSecs % 60:D2}";
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
        EventTypes.SecondYellow => "🟨🟥",
        EventTypes.PenaltySaved => "🧤",
        EventTypes.PenaltyMissed => "❌",
        EventTypes.SubIn => "🔄",
        _ => ""
    };

    private static bool IsSecondHalf(string? minute)
    {
        if (minute is null) return false;
        var num = new string(minute.TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(num, out var m) && m >= 46;
    }

    private static RenderFragment RenderDivider(string text) => builder =>
    {
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", "display:flex;align-items:center;padding:6px 14px;gap:8px;");
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", "flex:1;height:1px;background:var(--mud-palette-divider);");
        builder.CloseElement();
        builder.OpenElement(4, "span");
        builder.AddAttribute(5, "style", "font-size:11px;font-weight:700;color:var(--mud-palette-text-secondary);white-space:nowrap;");
        builder.AddContent(6, text);
        builder.CloseElement();
        builder.OpenElement(7, "div");
        builder.AddAttribute(8, "style", "flex:1;height:1px;background:var(--mud-palette-divider);");
        builder.CloseElement();
        builder.CloseElement();
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
        EventTypes.Goal => "<span style=\"font-size:13px;\">⚽</span>",
        EventTypes.PenaltyGoal => "<span style=\"font-size:13px;\">⚽</span>",
        EventTypes.OwnGoal => "<span style=\"font-size:13px;filter:grayscale(1) brightness(0.4) sepia(1) hue-rotate(-30deg) saturate(5);\">⚽</span>",
        EventTypes.Assist => "👟",
        EventTypes.YellowCard => MatchEventHelpers.YellowCardHtml,
        EventTypes.RedCard => MatchEventHelpers.RedCardHtml,
        EventTypes.SecondYellow => MatchEventHelpers.SecondYellowHtml,
        _ => ""
    };

    private static string LastName(string name)
    {
        var parts = name.Split(' ');
        return parts.Length > 1 ? parts[^1] : name;
    }

    private async Task NavigateToCompetition()
    {
        if (_match is null) return;
        var comps = await Api.GetCompetitionsAsync(CancellationToken.None);
        var comp = comps?.Data?.FirstOrDefault(c => c.Code == _match.CompetitionCode);
        if (comp is not null) Nav.NavigateTo($"/competitions/{comp.Id}");
    }

    private static string? ExtractVideoId(string? embedHtml)
    {
        if (embedHtml is null) return null;
        var match = System.Text.RegularExpressions.Regex.Match(embedHtml, @"embed/([a-zA-Z0-9_-]{11})");
        return match.Success ? match.Groups[1].Value : null;
    }

    private RenderFragment RenderPlayerCard(MatchPageLineupPlayer p, int size) => builder =>
    {
        var s = size;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", $"text-align:center;");

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

    private RenderFragment RenderSubCard(MatchPageLineupPlayer p) => builder =>
    {
        var isOn = p.SubMinute is not null;
        builder.OpenElement(0, "div");
        builder.AddAttribute(1, "style", "text-align:center;padding:6px 2px;");

        // Avatar
        builder.OpenElement(2, "div");
        builder.AddAttribute(3, "style", "position:relative;display:inline-block;");
        if (p.PhotoUrl is not null)
        {
            builder.OpenElement(4, "img");
            builder.AddAttribute(5, "src", p.PhotoUrl);
            builder.AddAttribute(6, "style", $"width:36px;height:36px;border-radius:50%;object-fit:cover;background:#333;{(isOn ? "" : "opacity:0.5;")}");
            builder.CloseElement();
        }
        else
        {
            builder.OpenElement(4, "div");
            builder.AddAttribute(5, "style", $"width:36px;height:36px;border-radius:50%;background:#555;display:flex;align-items:center;justify-content:center;{(isOn ? "" : "opacity:0.5;")}");
            builder.OpenElement(6, "span");
            builder.AddAttribute(7, "style", "font-size:12px;color:white;font-weight:700;");
            builder.AddContent(8, p.Name.Length > 0 ? p.Name[0].ToString() : "?");
            builder.CloseElement();
            builder.CloseElement();
        }
        // Sub-on badge
        if (isOn)
        {
            builder.OpenElement(9, "span");
            builder.AddAttribute(10, "style", "position:absolute;top:-4px;right:-6px;font-size:8px;background:#4caf50;color:white;border-radius:8px;padding:1px 4px;font-weight:700;line-height:1.2;");
            builder.AddContent(11, $"{p.SubMinute}'");
            builder.CloseElement();
        }
        builder.CloseElement(); // avatar container

        // Name
        builder.OpenElement(12, "div");
        builder.AddAttribute(13, "style", $"font-size:11px;font-weight:{(isOn ? "700" : "600")};margin-top:3px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;");
        if (p.ShirtNumber is not null)
            builder.AddContent(14, $"{p.ShirtNumber} ");
        builder.AddContent(15, LastName(p.Name));
        builder.CloseElement();

        // Position group
        var group = PlayerPositions.ToGroupName(p.Position);
        if (group.Length > 0)
        {
            builder.OpenElement(16, "div");
            builder.AddAttribute(17, "style", "font-size:10px;color:var(--mud-palette-text-secondary);opacity:0.7;");
            builder.AddContent(18, group);
            builder.CloseElement();
        }

        builder.CloseElement(); // outer div
    };

    private async Task LoadExtrasAsync()
    {
        try
        {
            var resp = await Api.GetMatchExtrasAsync(MatchId, CancellationToken.None);
            if (resp is { Success: true, Data: not null })
                _extras = resp.Data;
            else
                _extras = new MatchExtrasResult([], [], [], null, new CommunityPredictions(0, 0, 0, 0, null, 0), [], []);

            if (_match?.Status == nameof(MatchStatus.Finished))
            {
                var replayResp = await Api.GetPredictionReplayAsync(MatchId, 0, CancellationToken.None);
                if (replayResp is { Success: true, Data: not null })
                    _replay = replayResp.Data;
            }
        }
        catch
        {
            _extras = new MatchExtrasResult([], [], [], null, new CommunityPredictions(0, 0, 0, 0, null, 0), [], []);
        }
        await InvokeAsync(StateHasChanged);
    }

    private async Task LoadHighlightsAsync()
    {
        // For live matches, always refetch (new goals may appear)
        if (_highlights is not null && _match?.Status != nameof(MatchStatus.Live)) return;
        if (_match is null) return;
        _highlightsLoading = true;
        StateHasChanged();
        var resp = await Api.GetMatchHighlightsAsync(MatchId, CancellationToken.None);
        _highlights = resp is { Success: true, Data: not null } ? resp.Data : new MatchHighlightsResult([]);
        _highlightsLoading = false;
        await InvokeAsync(StateHasChanged);
    }

    private static string FormColor(string result) => result switch
    {
        "W" => "#4caf50",
        "D" => "#ff9800",
        "L" => "#f44336",
        _ => "#666"
    };

    private static string PredictionBg(string? outcome) => outcome switch
    {
        "ExactScore" => "#4caf50",
        "CorrectResultAndGoalDifference" => "#2196f3",
        "CorrectResult" => "#42a5f5",
        "CorrectGoalDifference" => "#ff9800",
        _ => "#666"
    };

    public void Dispose()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
        _clockTimer?.Stop();
        _clockTimer?.Dispose();
    }
}
