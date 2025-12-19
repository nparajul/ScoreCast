using ScoreCast.Models.V1.Requests.MasterData;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Models.V1.Responses.MasterData;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class MasterDataSync
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;

    private List<CompetitionResult> _competitions = [];
    private string? _newCompetitionCode;
    private bool _syncAllSeasons;
    private bool _pulseSyncing;
    private int _pulseProcessed;
    private int _pulseTotal;
    private const string AppName = "DATA SYNC";

    private readonly Dictionary<string, CompetitionSyncState> _syncStates = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await LoadCompetitionsAsync();
    }

    private CompetitionSyncState GetState(string code, bool isNew = false)
    {
        if (!_syncStates.TryGetValue(code, out var state))
        {
            // Existing competitions already have all base steps done
            var isPl = _competitions.Any(c => c.Code == code && c.ExternalSources.Contains("Fpl"));
            state = new CompetitionSyncState { CompletedSteps = isNew ? 0 : (isPl ? 4 : 3) };
            _syncStates[code] = state;
        }
        return state;
    }

    private static string StepAvatarStyle(bool completed) =>
        completed
            ? "background:var(--mud-palette-success);color:white;font-size:12px;font-weight:700;"
            : "background:var(--mud-palette-surface);border:2px solid var(--mud-palette-lines-default);color:var(--mud-palette-text-secondary);font-size:12px;font-weight:700;";

    private static string StepOpacity(int completedSteps, int requiredStep) =>
        completedSteps >= requiredStep ? "" : "opacity:0.45;pointer-events:none;";

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

    private async Task RunStepAsync(CompetitionResult competition, int step)
    {
        var state = GetState(competition.Code);
        state.LastMessage = null;

        try
        {
            switch (step)
            {
                case 1:
                    await SyncCompetitionAsync(competition, state);
                    break;
                case 2:
                    await SyncTeamsAsync(competition, state);
                    break;
                case 3:
                    await SyncMatchesAsync(competition, state);
                    break;
                case 4:
                    await SyncFplDataAsync(competition, state);
                    break;
            }
        }
        catch (Exception ex)
        {
            state.LastSuccess = false;
            state.LastMessage = ex.Message;
            await Alert.ShowDialogForException(ex, Severity.Error);
        }

        StateHasChanged();
    }

    private async Task SyncCompetitionAsync(CompetitionResult competition, CompetitionSyncState state)
    {
        await Loading.While(async () =>
        {
            var result = await Api.SyncCompetitionAsync(
                new SyncCompetitionRequest { CompetitionCode = competition.Code, AppName = AppName },
                CancellationToken.None);

            state.LastSuccess = result.Success;
            state.LastMessage = result.Message ?? (result.Success ? $"Synced {competition.Name}" : "Sync failed");
            if (result.Success && state.CompletedSteps < 1) state.CompletedSteps = 1;
        }, $"Step 1: Syncing {competition.Name}...");

        await LoadCompetitionsAsync();
    }

    private async Task SyncTeamsAsync(CompetitionResult competition, CompetitionSyncState state)
    {
        await Loading.While(async () =>
        {
            var result = await Api.SyncTeamsAsync(
                new SyncCompetitionRequest { CompetitionCode = competition.Code, AppName = AppName },
                CancellationToken.None);

            state.LastSuccess = result.Success;
            state.LastMessage = result.Message ?? (result.Success ? $"Synced teams for {competition.Name}" : "Team sync failed");
            if (result.Success && state.CompletedSteps < 2) state.CompletedSteps = 2;
        }, $"Step 2: Syncing teams for {competition.Name}...");
    }

    private async Task SyncMatchesAsync(CompetitionResult competition, CompetitionSyncState state)
    {
        await Loading.While(async () =>
        {
            var result = await Api.SyncMatchesAsync(
                new SyncCompetitionRequest { CompetitionCode = competition.Code, SyncAll = _syncAllSeasons, AppName = AppName },
                CancellationToken.None);

            state.LastSuccess = result.Success;
            state.LastMessage = result.Message ?? (result.Success ? $"Synced matches for {competition.Name}" : "Match sync failed");
            if (result.Success && state.CompletedSteps < 3) state.CompletedSteps = 3;
        }, $"Step 3: Syncing matches for {competition.Name}...");
    }

    private async Task SyncFplDataAsync(CompetitionResult competition, CompetitionSyncState state)
    {
        await Loading.While(async () =>
        {
            var result = await Api.SyncFplDataAsync(
                new SyncCompetitionRequest { CompetitionCode = competition.Code, AppName = AppName },
                CancellationToken.None);

            if (!result.Success)
            {
                state.LastSuccess = false;
                state.LastMessage = result.Message ?? "FPL sync failed";
                return;
            }
        }, $"Step 4a: Syncing FPL data for {competition.Name}...");

        if (!GetState(competition.Code).LastSuccess.GetValueOrDefault(true))
            return;

        await SyncPulseEventsAsync(competition, state);
    }

    private async Task SyncPulseEventsAsync(CompetitionResult competition, CompetitionSyncState state)
    {
        _pulseSyncing = true;
        _pulseProcessed = 0;
        _pulseTotal = 0;
        StateHasChanged();

        try
        {
            var totalEvents = 0;
            while (true)
            {
                ScoreCastResponse<SyncPulseEventsResult>? result = null;
                await Loading.While(async () =>
                {
                    result = await Api.SyncPulseEventsAsync(
                        new SyncPulseEventsRequest { CompetitionCode = competition.Code, BatchSize = 50, AppName = AppName },
                        CancellationToken.None);
                }, $"Step 4b: Syncing Pulse events ({_pulseProcessed}/{_pulseTotal})...");

                if (!result!.Success)
                {
                    state.LastSuccess = false;
                    state.LastMessage = result.Message ?? "Pulse sync failed";
                    return;
                }

                var data = result.Data!;
                if (_pulseTotal == 0) _pulseTotal = data.Total;
                _pulseProcessed += data.Processed;
                totalEvents += data.EventsAdded;
                StateHasChanged();
                await Task.Delay(50);

                if (data.Complete || data.Processed == 0) break;
            }

            state.LastSuccess = true;
            state.LastMessage = $"Synced {totalEvents} events from Pulse for {competition.Name}";
            if (state.CompletedSteps < 4) state.CompletedSteps = 4;
        }
        finally
        {
            _pulseSyncing = false;
            StateHasChanged();
        }
    }

    private async Task SyncNewCompetitionAsync()
    {
        if (string.IsNullOrWhiteSpace(_newCompetitionCode)) return;

        var code = _newCompetitionCode.Trim().ToUpperInvariant();
        _newCompetitionCode = null;

        try
        {
            // Run steps 1-3 automatically for new competitions
            var fakeCompetition = new CompetitionResult(0, code, code, null, "", null, []);
            var state = GetState(code, isNew: true);

            await SyncCompetitionAsync(fakeCompetition, state);
            if (!state.LastSuccess.GetValueOrDefault()) return;

            // Reload to get real competition data
            var real = _competitions.FirstOrDefault(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
            if (real is null) return;

            await SyncTeamsAsync(real, state);
            if (!state.LastSuccess.GetValueOrDefault()) return;

            await SyncMatchesAsync(real, state);
        }
        catch (Exception ex)
        {
            await Alert.ShowDialogForException(ex, Severity.Error);
        }
    }

    private async Task SyncAllCompetitionsAsync()
    {
        foreach (var competition in _competitions)
        {
            var state = GetState(competition.Code);

            await SyncCompetitionAsync(competition, state);
            if (!state.LastSuccess.GetValueOrDefault()) continue;

            await SyncTeamsAsync(competition, state);
            if (!state.LastSuccess.GetValueOrDefault()) continue;

            await SyncMatchesAsync(competition, state);
            StateHasChanged();
        }
    }

    private async Task CalculatePointsAsync()
    {
        await Loading.While(async () =>
        {
            var errors = new List<string>();
            foreach (var competition in _competitions)
            {
                var seasonsResponse = await Api.GetSeasonsAsync(competition.Code, CancellationToken.None);
                var season = seasonsResponse?.Data?.FirstOrDefault(s => s.IsCurrent);
                if (season is null) continue;

                var result = await Api.CalculateOutcomesAsync(
                    new CalculateOutcomesRequest { SeasonId = season.Id },
                    CancellationToken.None);

                if (!result.Success)
                    errors.Add($"{competition.Name}: {result.Message}");
            }

            if (errors.Count == 0)
                Alert.Add("Points calculated for all competitions", Severity.Success);
            else
                Alert.Add(string.Join("; ", errors), Severity.Error);
        }, "Calculating points...");

        StateHasChanged();
    }

    private async Task EnhanceLiveMatchesAsync()
    {
        await Loading.While(async () =>
        {
            var result = await Api.EnhanceLiveMatchesAsync(new EnhanceLiveMatchesRequest(), CancellationToken.None);
            if (result.Success)
                Alert.Add(result.Message ?? "Live matches enhanced", Severity.Success);
            else
                Alert.Add(result.Message ?? "Enhancement failed", Severity.Error);
        }, "Enhancing live matches...");

        StateHasChanged();
    }

    private sealed class CompetitionSyncState
    {
        public int CompletedSteps { get; set; }
        public bool? LastSuccess { get; set; }
        public string? LastMessage { get; set; }
    }
}
