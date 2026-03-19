using Microsoft.JSInterop;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Constants;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Dashboard
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
    private List<PredictionTile> _predictionTiles = [];
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
            var leaguesResponse = await Api.GetMyLeaguesAsync(CancellationToken.None);
            if (leaguesResponse is { Success: true, Data: not null })
                _leagues = leaguesResponse.Data;

            var competitionsResponse = await Api.GetCompetitionsAsync(CancellationToken.None);
            if (competitionsResponse is { Success: true, Data: not null })
                _competitions = competitionsResponse.Data;

            // Build prediction tiles from all competitions with current seasons
            foreach (var comp in _competitions)
            {
                var seasonsResponse = await Api.GetSeasonsAsync(comp.Code, CancellationToken.None);
                var currentSeason = seasonsResponse?.Data?.FirstOrDefault(s => s.IsCurrent);
                if (currentSeason is not null)
                {
                    _predictionTiles.Add(new PredictionTile(comp.Id, comp.Name, comp.Code,
                        comp.LogoUrl, currentSeason.Id, currentSeason.Name));
                }
            }

            _initialized = true;
        });
        StateHasChanged();
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
                var isNewCompetition = _predictionTiles.All(t => t.CompetitionId != response.Data.CompetitionId);
                _leagues.Add(response.Data);

                if (isNewCompetition)
                    Alert.Add($"Joined '{response.Data.Name}'! This is a {response.Data.CompetitionName} league — head to Predict Now to start making predictions.", Severity.Info);
                else
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

    private void NavigateToLeague(long leagueId) => Nav.NavigateTo($"/dashboard/{leagueId}");

    private void NavigateToPredict(string competitionCode, long seasonId) =>
        Nav.NavigateTo($"/predict?competition={competitionCode}&seasonId={seasonId}");

    private void AddPredictionForCompetition()
    {
        if (_selectedCompetition is null) return;
        _showAddPredictionDialog = false;

        // If user already has a tile for this competition, just navigate
        var existing = _predictionTiles.FirstOrDefault(t => t.CompetitionId == _selectedCompetition.Id);
        if (existing is not null)
        {
            NavigateToPredict(existing.CompetitionCode, existing.SeasonId);
            return;
        }

        // Otherwise navigate — the predict page will handle loading the season
        Nav.NavigateTo($"/predict?competition={_selectedCompetition.Code}");
        _selectedCompetition = null;
    }

    private record PredictionTile(long CompetitionId, string CompetitionName, string CompetitionCode,
        string? LogoUrl, long SeasonId, string? SeasonName);
}
