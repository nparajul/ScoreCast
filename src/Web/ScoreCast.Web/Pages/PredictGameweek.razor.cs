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

    [Parameter] public string? CompetitionCode { get; set; }
    [SupplyParameterFromQuery] public long? SeasonId { get; set; }

    private GameweekMatchesResult? _gameweek;
    private List<PredictionMatchViewModel> _matches = [];
    private List<ScoringRuleResult> _scoringRules = [];
    private long _seasonId;
    private string? _competitionName;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await Loading.While(async () =>
        {
            var code = CompetitionCode ?? CompetitionCodes.PremierLeague;

            if (SeasonId is > 0)
            {
                _seasonId = SeasonId.Value;
            }
            else
            {
                var seasonsResponse = await Api.GetSeasonsAsync(code, CancellationToken.None);
                var season = seasonsResponse?.Data?.FirstOrDefault(s => s.IsCurrent);
                if (season is null)
                {
                    Alert.Add("No active season found", Severity.Error);
                    return;
                }
                _seasonId = season.Id;
            }

            var compResponse = await Api.GetCompetitionsAsync(CancellationToken.None);
            _competitionName = compResponse?.Data?.FirstOrDefault(c => c.Code == code)?.Name;

            var rulesResponse = await Api.GetScoringRulesAsync(CancellationToken.None);
            if (rulesResponse is { Success: true, Data: not null })
                _scoringRules = rulesResponse.Data;

            await LoadGameweek(SharedConstants.CurrentGameweek);
        });
        StateHasChanged();
    }

    private async Task LoadGameweek(int gameweekNumber)
    {
        var response = await Api.GetGameweekMatchesAsync(_seasonId, gameweekNumber, CancellationToken.None);
        if (response is { Success: true, Data: not null })
        {
            _gameweek = response.Data;
            _matches = _gameweek.Matches.Select(PredictionMatchViewModel.FromMatch).ToList();

            var predictionsResponse = await Api.GetMyPredictionsAsync(_seasonId, _gameweek.GameweekId, CancellationToken.None);
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
                new SubmitPredictionsRequest { SeasonId = _seasonId, Predictions = entries },
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
