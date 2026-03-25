using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Types;

namespace ScoreCast.Web.Pages;

public partial class GlobalDashboard : IDisposable
{
    [Inject] private ApiClient.V1.Apis.IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private GlobalDashboardResult? _data;
    private bool _loaded;
    private string _countdownText = "";
    private bool _allLocked;
    private Timer? _timer;
    private List<CompetitionResult> _competitions = [];
    private CompetitionResult? _selectedCompetition;

    protected override async Task OnInitializedAsync()
    {
        var compsTask = Api.GetCompetitionsAsync(default);
        var defaultTask = Api.GetDefaultCompetitionAsync(default);
        await Task.WhenAll(compsTask, defaultTask);

        if (compsTask.Result is { Success: true, Data: not null })
            _competitions = compsTask.Result.Data;

        var defaultCode = defaultTask.Result is { Success: true, Data: not null }
            ? defaultTask.Result.Data.Code : null;
        _selectedCompetition = _competitions.FirstOrDefault(c => c.Code == defaultCode) ?? _competitions.FirstOrDefault();

        await LoadData();
        _timer = new Timer(_ => { UpdateCountdown(); InvokeAsync(StateHasChanged); }, null, 0, 1000);
    }

    private async Task OnCompetitionChanged(CompetitionResult comp)
    {
        _selectedCompetition = comp;
        _loaded = false;
        StateHasChanged();
        await LoadData();
        StateHasChanged();
    }

    private async Task LoadData()
    {
        var result = await Api.GetGlobalDashboardAsync(_selectedCompetition?.Code, default);
        if (result is { Success: true, Data: not null })
            _data = result.Data;
        _loaded = true;
    }

    private void UpdateCountdown()
    {
        if (_data is null) return;
        var diff = _data.Countdown.Deadline.ToUniversalTime() - ScoreCastDateTime.Now.Value;
        if (diff.TotalSeconds <= 0)
        {
            _countdownText = _data.Countdown.IsComplete ? "Gameweek complete ✅" : "Gameweek in progress";
            _allLocked = true;
        }
        else
        {
            var parts = new List<string>();
            if ((int)diff.TotalDays > 0) parts.Add($"{(int)diff.TotalDays}d");
            if (diff.Hours > 0 || parts.Count > 0) parts.Add($"{diff.Hours}h");
            if (diff.Minutes > 0 || parts.Count > 0) parts.Add($"{diff.Minutes}m");
            parts.Add($"{diff.Seconds}s");
            _countdownText = string.Join(" ", parts);
            _allLocked = false;
        }
    }

    public void Dispose() => _timer?.Dispose();
}
