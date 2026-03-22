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

    protected override async Task OnInitializedAsync()
    {
        var result = await Api.GetGlobalDashboardAsync(default);
        if (result is { Success: true, Data: not null })
            _data = result.Data;
        _loaded = true;

        _timer = new Timer(_ => { UpdateCountdown(); InvokeAsync(StateHasChanged); }, null, 0, 1000);
    }

    private void UpdateCountdown()
    {
        if (_data is null) return;
        var diff = _data.Countdown.Deadline.ToUniversalTime() - ScoreCastDateTime.Now.Value;
        if (diff.TotalSeconds <= 0)
        {
            _countdownText = "Gameweek in progress";
            _allLocked = true;
        }
        else
        {
            _countdownText = $"{(int)diff.TotalDays}d {diff.Hours}h {diff.Minutes}m {diff.Seconds}s";
            _allLocked = false;
        }
    }

    private static string ShortName(string name) =>
        name.Replace(" FC", "").Replace(" AFC", "").Trim();

    public void Dispose() => _timer?.Dispose();
}
