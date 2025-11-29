using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Requests.Football;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class FootballDataSync
{
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;

    private List<CompetitionResult> _competitions = [];
    private string? _newCompetitionCode;

    private bool _loaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _loaded) return;
        _loaded = true;
        await InvokeAsync(LoadCompetitionsAsync);
    }

    private async Task LoadCompetitionsAsync()
    {
        ScoreCastResponse<List<CompetitionResult>>? response = null;
        await Loading.While(async () => response = await Api.GetCompetitionsAsync(CancellationToken.None));
        if (response is { Success: true, Data: not null })
            _competitions = response.Data;
        else
            Alert.Add("No competitions found", Severity.Error);

        StateHasChanged();
    }

    private async Task SyncNewCompetitionAsync()
    {
        if (string.IsNullOrWhiteSpace(_newCompetitionCode)) return;

        try
        {
            var code = _newCompetitionCode.Trim();
            await Loading.While(async () =>
            {
                var result = await Api.SyncCompetitionAsync(new SyncCompetitionRequest { CompetitionCode = code }, CancellationToken.None);
                if (result.Success)
                    Alert.Add(result.Message ?? $"Synced {code} successfully", Severity.Success);
                else
                    Alert.Add(result.Message ?? "Sync failed", Severity.Error);
            });

            _newCompetitionCode = null;
            await LoadCompetitionsAsync();
        }
        catch (Exception ex)
        {
            await Alert.ShowDialogForException(ex, Severity.Error);
        }
    }

    private async Task SyncCompetitionAsync(CompetitionResult competition)
    {
        try
        {
            await Loading.While(async () =>
            {
                var result = await Api.SyncCompetitionAsync(new SyncCompetitionRequest { CompetitionCode = competition.Code }, CancellationToken.None);
                if (result.Success)
                    Alert.Add(result.Message ?? $"Synced {competition.Name} successfully", Severity.Success);
                else
                    Alert.Add(result.Message ?? "Sync failed", Severity.Error);
            });
        }
        catch (Exception ex)
        {
            await Alert.ShowDialogForException(ex, Severity.Error);
        }
    }

    private async Task SyncTeamsAsync(CompetitionResult competition)
    {
        try
        {
            await Loading.While(async () =>
            {
                var result = await Api.SyncTeamsAsync(new SyncCompetitionRequest { CompetitionCode = competition.Code }, CancellationToken.None);
                if (result.Success)
                    Alert.Add(result.Message ?? $"Synced teams for {competition.Name}", Severity.Success);
                else
                    Alert.Add(result.Message ?? "Team sync failed", Severity.Error);
            });
        }
        catch (Exception ex)
        {
            await Alert.ShowDialogForException(ex, Severity.Error);
        }
    }

    private async Task SyncMatchesAsync(CompetitionResult competition)
    {
        try
        {
            await Loading.While(async () =>
            {
                var result = await Api.SyncMatchesAsync(new SyncCompetitionRequest { CompetitionCode = competition.Code }, CancellationToken.None);
                if (result.Success)
                    Alert.Add(result.Message ?? $"Synced matches for {competition.Name}", Severity.Success);
                else
                    Alert.Add(result.Message ?? "Match sync failed", Severity.Error);
            });
        }
        catch (Exception ex)
        {
            await Alert.ShowDialogForException(ex, Severity.Error);
        }
    }
}
