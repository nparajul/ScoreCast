using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Constants;
using ScoreCast.Web.Components.Helpers;
using ScoreCast.Web.ViewModels;

namespace ScoreCast.Web.Pages;

public partial class PredictGameweek
{
    private const string _appName = "PREDICT GAMEWEEK";
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    [Parameter] public long SeasonId { get; set; }

    private GameweekMatchesResult? _gameweek;
    private List<PredictionMatchViewModel> _matches = [];
    private List<ScoringRuleResult> _scoringRules = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        if (SeasonId <= 0)
        {
            Nav.NavigateTo("/dashboard");
            return;
        }

        await Loading.While(async () =>
        {
            var rulesResponse = await Api.GetScoringRulesAsync(CancellationToken.None);
            if (rulesResponse is { Success: true, Data: not null })
                _scoringRules = rulesResponse.Data;

            await LoadGameweek(SharedConstants.CurrentGameweek);
        });
        StateHasChanged();
    }

    private async Task LoadGameweek(int gameweekNumber)
    {
        var response = await Api.GetGameweekMatchesAsync(SeasonId, gameweekNumber, CancellationToken.None);
        if (response is { Success: true, Data: not null })
        {
            _gameweek = response.Data;
            _matches = _gameweek.Matches.Select(PredictionMatchViewModel.FromMatch).ToList();

            var predictionsResponse = await Api.GetMyPredictionsAsync(SeasonId, _gameweek.GameweekId, CancellationToken.None);
            if (predictionsResponse is { Success: true, Data: not null })
            {
                foreach (var prediction in predictionsResponse.Data)
                {
                    var match = _matches.FirstOrDefault(m => m.MatchId == prediction.MatchId);
                    if (match is not null)
                    {
                        match.PredictedHomeScore = prediction.PredictedHomeScore;
                        match.PredictedAwayScore = prediction.PredictedAwayScore;
                        match.Outcome = prediction.Outcome?.ToString();
                        match.HasSavedPrediction = true;
                    }
                }
            }
        }
    }

    private async Task PreviousGameweek()
    {
        if (_gameweek is null || _gameweek.GameweekNumber <= 1) return;
        await Loading.While(async () => await LoadGameweek(_gameweek.GameweekNumber - 1));
    }

    private async Task NextGameweek()
    {
        if (_gameweek is null || _gameweek.GameweekNumber >= _gameweek.TotalGameweeks) return;
        await Loading.While(async () => await LoadGameweek(_gameweek.GameweekNumber + 1));
    }

    private async Task SubmitPredictionsAsync()
    {
        if (_gameweek is null) return;

        var validationError = PredictionMatchViewModel.Validate(_matches);
        if (validationError is not null)
        {
            Alert.Add(validationError, Severity.Error);
            return;
        }

        var entries = _matches
            .Where(m => !m.IsLocked && m.HasPrediction)
            .Select(m => new PredictionEntry
            {
                MatchId = m.MatchId,
                PredictedHomeScore = m.PredictedHomeScore!.Value,
                PredictedAwayScore = m.PredictedAwayScore!.Value
            })
            .ToList();

        await Loading.While(async () =>
        {
            var response = await Api.SubmitPredictionsAsync(
                new SubmitPredictionsRequest { SeasonId = SeasonId, Predictions = entries },
                CancellationToken.None);

            if (response is { Success: true })
            {
                foreach (var m in _matches.Where(m => !m.IsLocked && m.HasPrediction))
                    m.HasSavedPrediction = true;
                Alert.Add(response.Message ?? "Predictions saved!", Severity.Success);
            }
            else
                Alert.Add(response?.Message ?? "Failed to save predictions", Severity.Error);
        });
    }
}
