using Microsoft.AspNetCore.Components;
using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Web.Components.Helpers;
using ScoreCast.Web.ViewModels;

namespace ScoreCast.Web.Pages;

public partial class PredictGameweek
{
    private const string AppName = "PREDICT GAMEWEEK";
    [Parameter] public long LeagueId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private GameweekMatchesResult? _gameweek;
    private List<PredictionMatchViewModel> _matches = [];
    private long _seasonId;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await InvokeAsync(async () =>
        {
            await Loading.While(async () =>
            {
                var leaguesResponse = await Api.GetMyLeaguesAsync(CancellationToken.None);
                var league = leaguesResponse?.Data?.FirstOrDefault(l => l.Id == LeagueId);
                if (league is null)
                {
                    Alert.Add("You are not a member of this league", Severity.Error);
                    return;
                }
                _seasonId = league.SeasonId;

                await LoadGameweek(SharedConstants.CurrentGameweek);
            });
            StateHasChanged();
        });
    }

    private async Task LoadGameweek(int gameweekNumber)
    {
        var response = await Api.GetGameweekMatchesAsync(_seasonId, gameweekNumber, CancellationToken.None);
        if (response is { Success: true, Data: not null })
        {
            _gameweek = response.Data;
            _matches = _gameweek.Matches.Select(PredictionMatchViewModel.FromMatch).ToList();

            var predictionsResponse = await Api.GetMyPredictionsAsync(LeagueId, _gameweek.GameweekId, CancellationToken.None);
            if (predictionsResponse is { Success: true, Data: not null })
            {
                foreach (var prediction in predictionsResponse.Data)
                {
                    var match = _matches.FirstOrDefault(m => m.MatchId == prediction.MatchId);
                    if (match is not null)
                    {
                        match.PredictedHomeScore = prediction.PredictedHomeScore;
                        match.PredictedAwayScore = prediction.PredictedAwayScore;
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
                new SubmitPredictionsRequest { PredictionLeagueId = LeagueId, Predictions = entries },
                CancellationToken.None);

            if (response is { Success: true })
                Alert.Add(response.Message ?? "Predictions saved!", Severity.Success);
            else
                Alert.Add(response?.Message ?? "Failed to save predictions", Severity.Error);
        });
    }
}
