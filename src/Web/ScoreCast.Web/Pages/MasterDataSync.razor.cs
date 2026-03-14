using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Requests.MasterData;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Models.V1.Responses.MasterData;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class MasterDataSync
{
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;

    private List<CompetitionResult> _competitions = [];
    private string? _newCompetitionCode;
    private const string AppName = "DATA SYNC";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
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
                var result = await Api.SyncCompetitionAsync(new SyncCompetitionRequest { CompetitionCode = code, AppName = AppName }, CancellationToken.None);
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
                var result = await Api.SyncCompetitionAsync(new SyncCompetitionRequest { CompetitionCode = competition.Code, AppName = AppName }, CancellationToken.None);
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
                var result = await Api.SyncTeamsAsync(new SyncCompetitionRequest { CompetitionCode = competition.Code, AppName = AppName }, CancellationToken.None);
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
                var result = await Api.SyncMatchesAsync(new SyncCompetitionRequest { CompetitionCode = competition.Code, AppName = AppName }, CancellationToken.None);
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

    private async Task SyncFplDataAsync(CompetitionResult competition)
    {
        try
        {
            await Loading.While(async () =>
            {
                var result = await Api.SyncFplDataAsync(new SyncCompetitionRequest { CompetitionCode = competition.Code, AppName = AppName }, CancellationToken.None);
                if (result.Success)
                    Alert.Add(result.Message ?? $"Synced FPL mappings for {competition.Name}", Severity.Success);
                else
                    Alert.Add(result.Message ?? "FPL sync failed", Severity.Error);
            });

            await SyncPulseEventsAsync(competition);
        }
        catch (Exception ex)
        {
            await Alert.ShowDialogForException(ex, Severity.Error);
        }
    }

    private bool _pulseSyncing;
    private int _pulseProcessed;
    private int _pulseTotal;

    private async Task SyncPulseEventsAsync(CompetitionResult competition)
    {
        _pulseSyncing = true;
        _pulseProcessed = 0;
        _pulseTotal = 0;
        StateHasChanged();

        try
        {
            var totalEvents = 0;
            var failed = false;
            while (true)
            {
                ScoreCastResponse<SyncPulseEventsResult>? result = null;
                await Loading.While(async () =>
                {
                    result = await Api.SyncPulseEventsAsync(
                        new SyncPulseEventsRequest { CompetitionCode = competition.Code, BatchSize = 50, AppName = AppName },
                        CancellationToken.None);
                }, $"Syncing Pulse events ({_pulseProcessed}/{_pulseTotal})...");

                if (!result!.Success)
                {
                    Alert.Add(result.Message ?? "Pulse sync failed", Severity.Error);
                    failed = true;
                    break;
                }

                var data = result.Data!;
                if (_pulseTotal == 0) _pulseTotal = data.Total;
                _pulseProcessed += data.Processed;
                totalEvents += data.EventsAdded;
                StateHasChanged();
                await Task.Delay(50);

                if (data.Complete || data.Processed == 0) break;
            }

            if (!failed)
                Alert.Add($"Synced {totalEvents} events from Pulse for {competition.Name}", Severity.Success);
        }
        catch (Exception ex)
        {
            await Alert.ShowDialogForException(ex, Severity.Error);
        }
        finally
        {
            _pulseSyncing = false;
            StateHasChanged();
        }
    }
}
