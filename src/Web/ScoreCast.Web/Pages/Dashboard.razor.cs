using Microsoft.JSInterop;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Types;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Dashboard : IDisposable
{
    private const string _appName = "DASHBOARD";
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private List<PredictionLeagueResult> _leagues = [];
    private List<CompetitionResult> _competitions = [];
    private List<UserSeasonResult> _userSeasons = [];
    private MyPredictionStatsResult? _stats;
    private GlobalDashboardResult? _globalData;
    private bool _initialized;
    private bool _showCreateDialog;
    private bool _showJoinDialog;
    private bool _showAddPredictionDialog;
    private string? _newLeagueName;
    private CompetitionResult? _selectedCompetition;
    private string? _inviteCode;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await Loading.While(async () =>
        {
            var leaguesTask = Api.GetMyLeaguesAsync(CancellationToken.None);
            var competitionsTask = Api.GetCompetitionsAsync(CancellationToken.None);
            var userSeasonsTask = Api.GetUserSeasonsAsync(CancellationToken.None);
            var statsTask = Api.GetMyPredictionStatsAsync(CancellationToken.None);

            await Task.WhenAll(leaguesTask, competitionsTask, userSeasonsTask, statsTask);

            if (leaguesTask.Result is { Success: true, Data: not null })
                _leagues = leaguesTask.Result.Data;
            if (competitionsTask.Result is { Success: true, Data: not null })
                _competitions = competitionsTask.Result.Data;
            if (userSeasonsTask.Result is { Success: true, Data: not null })
                _userSeasons = userSeasonsTask.Result.Data;
            if (statsTask.Result is { Success: true, Data: not null })
                _stats = statsTask.Result.Data;

            // Load global data for deadline urgency (non-blocking)
            _ = LoadGlobalDataAsync();

            _initialized = true;
        });
        StateHasChanged();
        _dotnetRef = DotNetObjectReference.Create(this);
        await Js.InvokeVoidAsync("touchDrag.init", "tile-container", _dotnetRef);
    }

    private async Task CreateLeagueAsync()
    {
        if (string.IsNullOrWhiteSpace(_newLeagueName) || _selectedCompetition is null) return;
        _showCreateDialog = false;

        await Loading.While(async () =>
        {
            var response = await Api.CreatePredictionLeagueAsync(
                new CreatePredictionLeagueRequest { Name = _newLeagueName, CompetitionId = _selectedCompetition.Id },
                CancellationToken.None);

            if (response is { Success: true, Data: not null })
            {
                _leagues.Add(response.Data);
                Alert.Add($"League '{response.Data.Name}' created! Invite code: {response.Data.InviteCode}", Severity.Success);
                _newLeagueName = null;
                _selectedCompetition = null;
            }
            else
            {
                Alert.Add(response?.Message ?? "Failed to create league", Severity.Error);
            }
        });
    }

    private async Task JoinLeagueAsync()
    {
        if (string.IsNullOrWhiteSpace(_inviteCode)) return;
        _showJoinDialog = false;

        await Loading.While(async () =>
        {
            var response = await Api.JoinPredictionLeagueAsync(
                new JoinPredictionLeagueRequest { InviteCode = _inviteCode.Trim().ToUpper() },
                CancellationToken.None);

            if (response is { Success: true, Data: not null })
            {
                _leagues.Add(response.Data);
                Alert.Add($"Joined '{response.Data.Name}'!", Severity.Success);
                _inviteCode = null;
            }
            else
            {
                Alert.Add(response?.Message ?? "Failed to join league", Severity.Error);
            }
        });
    }

    private async Task AddPredictionForCompetition()
    {
        if (_selectedCompetition is null) return;
        _showAddPredictionDialog = false;

        var existing = _userSeasons.FirstOrDefault(us => us.CompetitionId == _selectedCompetition.Id);
        if (existing is not null)
        {
            Nav.NavigateTo($"/predict/{existing.SeasonId}");
            _selectedCompetition = null;
            return;
        }

        // Find current season for selected competition
        var seasonsResponse = await Api.GetSeasonsAsync(_selectedCompetition.Code, CancellationToken.None);
        var currentSeason = seasonsResponse?.Data?.FirstOrDefault(s => s.IsCurrent);
        if (currentSeason is null)
        {
            Alert.Add("No active season found for this competition", Severity.Error);
            _selectedCompetition = null;
            return;
        }

        await Loading.While(async () =>
        {
            var response = await Api.EnrollUserSeasonAsync(
                new EnrollUserSeasonRequest { SeasonId = currentSeason.Id },
                CancellationToken.None);

            if (response is { Success: true, Data: not null })
            {
                _userSeasons.Add(response.Data);
                Nav.NavigateTo($"/predict/{currentSeason.Id}");
            }
            else
            {
                Alert.Add(response?.Message ?? "Failed to add competition", Severity.Error);
            }
        });

        _selectedCompetition = null;
    }

    private async Task CopyInviteCode(string code)
    {
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", code);
        Snackbar.Add("Invite code copied!", Severity.Success);
    }

    private void NavigateToLeague(long leagueId) => Nav.NavigateTo($"/dashboard/{leagueId}");

    private int _dragIndex = -1;
    private DotNetObjectReference<Dashboard>? _dotnetRef;

    [JSInvokable]
    public async Task JsDragEnter(int targetIndex)
    {
        OnDragEnter(targetIndex);
        await InvokeAsync(StateHasChanged);
    }

    [JSInvokable]
    public async Task JsDragEnd()
    {
        _dragIndex = -1;
        await SaveOrder();
        StateHasChanged();
    }

    private async Task MoveItem(int from, int to)
    {
        var item = _userSeasons[from];
        _userSeasons.RemoveAt(from);
        _userSeasons.Insert(to, item);
        await SaveOrder();
    }

    private void OnDragEnter(int targetIndex)
    {
        if (_dragIndex < 0 || _dragIndex == targetIndex) return;
        var item = _userSeasons[_dragIndex];
        _userSeasons.RemoveAt(_dragIndex);
        _userSeasons.Insert(targetIndex, item);
        _dragIndex = targetIndex;
    }

    private async Task OnDragEnd()
    {
        _dragIndex = -1;
        await SaveOrder();
    }

    private async Task SaveOrder()
    {
        await Api.ReorderUserSeasonsAsync(
            new ReorderUserSeasonsRequest { SeasonIds = _userSeasons.Select(s => s.SeasonId).ToList() },
            CancellationToken.None);
    }

    public void Dispose() => _dotnetRef?.Dispose();

    private async Task LoadGlobalDataAsync()
    {
        try
        {
            var resp = await Api.GetGlobalDashboardAsync(null, CancellationToken.None);
            if (resp is { Success: true, Data: not null })
            {
                _globalData = resp.Data;
                await InvokeAsync(StateHasChanged);
            }
        }
        catch { /* non-critical */ }
    }

    private string? DeadlineUrgency
    {
        get
        {
            if (_globalData?.Countdown is null) return null;
            var diff = _globalData.Countdown.Deadline.ToUniversalTime() - ScoreCastDateTime.Now.Value;
            if (diff.TotalHours <= 0) return null;
            if (diff.TotalHours <= 2) return $"⏰ Predictions lock in {(int)diff.TotalMinutes} minutes! {_globalData.Countdown.TotalUsers} others already in.";
            if (diff.TotalHours <= 24) return $"⏰ GW{_globalData.Countdown.GameweekNumber} locks in {(int)diff.TotalHours}h — {_globalData.Countdown.TotalPredictions} predictions so far!";
            return null;
        }
    }
}
