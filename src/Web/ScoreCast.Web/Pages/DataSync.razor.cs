using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class DataSync
{
    [Inject] private IFootballApi FootballApi { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;

    private List<CompetitionResult> _competitions = [];
    private string? _newCompetitionCode;

    protected override async Task OnInitializedAsync() => await LoadCompetitionsAsync();

    private async Task LoadCompetitionsAsync()
    {
        await Loading.While(async () =>
        {
            var response = await FootballApi.GetCompetitionsAsync(CancellationToken.None);
            if (response.Success && response.Data is not null)
                _competitions = response.Data;
        });
    }

    private async Task SyncNewCompetitionAsync()
    {
        if (string.IsNullOrWhiteSpace(_newCompetitionCode)) return;

        try
        {
            var code = _newCompetitionCode.Trim();
            await Loading.While(async () =>
            {
                var result = await FootballApi.SyncCompetitionAsync(code, CancellationToken.None);
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
                var result = await FootballApi.SyncCompetitionAsync(competition.Code, CancellationToken.None);
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
}
