using Microsoft.JSInterop;
using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Constants;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Leagues
{
    private const string AppName = "LEAGUES";
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    [Inject] private ISnackbar Snackbar { get; set; } = default!;
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private List<PredictionLeagueResult> _leagues = [];
    private long _currentSeasonId;
    private int _currentGameweek;
    private bool _initialized;
    private bool _showCreateDialog;
    private bool _showJoinDialog;
    private string? _newLeagueName;
    private string? _inviteCode;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await Loading.While(async () =>
        {
            var leaguesResponse = await Api.GetMyLeaguesAsync(CancellationToken.None);
            if (leaguesResponse is { Success: true, Data: not null })
                _leagues = leaguesResponse.Data;

            var seasonsResponse = await Api.GetSeasonsAsync(CompetitionCodes.PremierLeague, CancellationToken.None);
            if (seasonsResponse is { Success: true, Data: not null })
            {
                _currentSeasonId = seasonsResponse.Data.FirstOrDefault(s => s.IsCurrent)?.Id ?? 0;
                if (_currentSeasonId > 0)
                {
                    var gwResponse = await Api.GetGameweekMatchesAsync(_currentSeasonId, SharedConstants.CurrentGameweek, CancellationToken.None);
                    if (gwResponse is { Success: true, Data: not null })
                        _currentGameweek = gwResponse.Data.CurrentGameweek;
                }
            }

            _initialized = true;
        });
        StateHasChanged();
    }

    private async Task CreateLeagueAsync()
    {
        if (string.IsNullOrWhiteSpace(_newLeagueName) || _currentSeasonId == 0) return;
        _showCreateDialog = false;

        await Loading.While(async () =>
        {
            var response = await Api.CreatePredictionLeagueAsync(
                new CreatePredictionLeagueRequest { Name = _newLeagueName, SeasonId = _currentSeasonId },
                CancellationToken.None);

            if (response is { Success: true, Data: not null })
            {
                _leagues.Add(response.Data);
                Alert.Add($"League '{response.Data.Name}' created! Invite code: {response.Data.InviteCode}", Severity.Success);
                _newLeagueName = null;
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

    private async Task CopyInviteCode(string code)
    {
        await Js.InvokeVoidAsync("navigator.clipboard.writeText", code);
        Snackbar.Add("Invite code copied!", Severity.Success);
    }

    private void NavigateToLeague(long leagueId) => Nav.NavigateTo($"/leagues/{leagueId}");
}
