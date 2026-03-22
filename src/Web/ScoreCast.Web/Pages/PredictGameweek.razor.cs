using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Web.Components.Helpers;
using ScoreCast.Web.Validation.Prediction;
using ScoreCast.Web.ViewModels;

namespace ScoreCast.Web.Pages;

public partial class PredictGameweek
{
    private const string _appName = "PREDICT GAMEWEEK";
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private IClientTimeProvider ClientTime { get; set; } = null!;
    [Inject] private IDialogService Dialog { get; set; } = null!;

    [Parameter] public long SeasonId { get; set; }

    private GameweekMatchesResult? _gameweek;
    private List<PredictionMatchViewModel> _matches = [];
    private List<ScoringRuleResult> _scoringRules = [];
    private bool _showBreakdown;
    private readonly HashSet<string> _expandedRules = [];
    private bool _showRiskPlays;
    private List<RiskPlayViewModel> _riskPlays = [];
    private Dictionary<long, MatchPredictionSummary> _communityData = [];
    private bool _showConfidence;

    private void ToggleRule(string label)
    {
        if (!_expandedRules.Remove(label)) _expandedRules.Add(label);
    }

    private bool AllLocked => _matches.All(m => m.IsLocked);
    private List<PredictionMatchViewModel> UnlockedMatches => _matches.Where(m => !m.IsLocked).ToList();
    private string MatchLabel(long matchId) => _matches.FirstOrDefault(x => x.MatchId == matchId) is { } m ? $"{m.HomeTeamShortName} vs {m.AwayTeamShortName}" : "";

    private static void Increment(PredictionMatchViewModel m, bool home)
    {
        if (home) m.PredictedHomeScore = (m.PredictedHomeScore ?? -1) + 1;
        else m.PredictedAwayScore = (m.PredictedAwayScore ?? -1) + 1;
    }

    private static void Decrement(PredictionMatchViewModel m, bool home)
    {
        if (home) m.PredictedHomeScore = m.PredictedHomeScore > 0 ? m.PredictedHomeScore - 1 : 0;
        else m.PredictedAwayScore = m.PredictedAwayScore > 0 ? m.PredictedAwayScore - 1 : 0;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await ClientTime.InitializeAsync();

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

            var predictionsTask = Api.GetMyPredictionsAsync(SeasonId, _gameweek.GameweekId, CancellationToken.None);
            var riskPlaysTask = Api.GetMyRiskPlaysAsync(SeasonId, _gameweek.GameweekId, CancellationToken.None);
            await Task.WhenAll(predictionsTask, riskPlaysTask);

            if (predictionsTask.Result is { Success: true, Data: not null })
            {
                foreach (var prediction in predictionsTask.Result.Data)
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

            InitRiskPlays(riskPlaysTask.Result);
        }
    }

    private void InitRiskPlays(ScoreCastResponse<List<RiskPlayResult>>? response)
    {
        _riskPlays =
        [
            new() { RiskType = RiskPlayType.DoubleDown },
            new() { RiskType = RiskPlayType.ExactScoreBoost },
            new() { RiskType = RiskPlayType.CleanSheetBet },
            new() { RiskType = RiskPlayType.FirstGoalTeam },
            new() { RiskType = RiskPlayType.OverUnderGoals },
        ];
        if (response is not { Success: true, Data: not null }) return;
        foreach (var saved in response.Data)
        {
            var rp = _riskPlays.FirstOrDefault(r => r.RiskType == saved.RiskType);
            if (rp is null) continue;
            rp.MatchId = saved.MatchId;
            rp.Selection = saved.Selection;
            rp.BonusPoints = saved.BonusPoints;
            rp.IsWon = saved.IsWon;
            rp.IsResolved = saved.IsResolved;
        }
        _showRiskPlays = _riskPlays.Any(r => r.IsActive);
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

        var validator = new PredictionMatchViewModelValidator();
        var errors = _matches.Where(m => !m.IsLocked)
            .SelectMany(m => validator.Validate(m).Errors)
            .Select(e => e.ErrorMessage)
            .Distinct()
            .ToList();

        if (errors.Count > 0)
        {
            Alert.Add(string.Join(" ", errors), Severity.Error);
            return;
        }

        var missing = _matches.Count(m => !m.IsLocked && !m.HasPrediction);
        if (missing > 0)
        {
            Alert.Add($"Please enter predictions for all matches ({missing} remaining)", Severity.Error);
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

        // Nudge if no risk plays active
        if (!_riskPlays.Any(r => r.IsActive))
        {
            var proceed = await Dialog.ShowMessageBoxAsync(
                "No Risk Plays? 🎲",
                "Playing it safe? Risk plays can earn you bonus points — or cost you. Are you brave enough?",
                "Save without risks", cancelText: "Let me add some");
            if (proceed != true) { _showRiskPlays = true; return; }
        }

        await Loading.While(async () =>
        {
            var response = await Api.SubmitPredictionsAsync(
                new SubmitPredictionsRequest { SeasonId = SeasonId, Predictions = entries },
                CancellationToken.None);

            if (response is { Success: true })
            {
                foreach (var m in _matches.Where(m => !m.IsLocked && m.HasPrediction))
                    m.HasSavedPrediction = true;

                var riskEntries = _riskPlays.Where(r => r.IsActive)
                    .Select(r => new RiskPlayEntry { MatchId = r.MatchId!.Value, RiskType = r.RiskType, Selection = r.Selection })
                    .ToList();
                if (riskEntries.Count > 0)
                    await Api.SubmitRiskPlaysAsync(new SubmitRiskPlaysRequest { SeasonId = SeasonId, RiskPlays = riskEntries }, CancellationToken.None);

                Alert.Add(response.Message ?? "Predictions saved!",
                    response.Message?.Contains("skipped") == true ? Severity.Warning : Severity.Success);

                // Load community confidence data
                _ = LoadConfidenceAsync();
            }
            else
                Alert.Add(response?.Message ?? "Failed to save predictions", Severity.Error);
        });
    }

    private async Task LoadConfidenceAsync()
    {
        var resp = await Api.GetGlobalDashboardAsync(CancellationToken.None);
        if (resp is { Success: true, Data: not null })
        {
            _communityData = resp.Data.UpcomingPredictions.ToDictionary(p => p.MatchId);
            _showConfidence = true;
            await InvokeAsync(StateHasChanged);
        }
    }

    private string? GetConfidenceText(PredictionMatchViewModel match)
    {
        if (!_showConfidence || !match.HasPrediction) return null;
        if (!_communityData.TryGetValue(match.MatchId, out var community) || community.PredictionCount < 2) return null;

        var predictedScore = $"{match.PredictedHomeScore}-{match.PredictedAwayScore}";
        var isPopular = predictedScore == community.MostPredictedScore;

        // Determine if user's result matches majority
        var userResult = match.PredictedHomeScore > match.PredictedAwayScore ? "H"
            : match.PredictedHomeScore < match.PredictedAwayScore ? "A" : "D";
        var majorityPct = userResult switch
        {
            "H" => community.HomePct,
            "A" => community.AwayPct,
            _ => community.DrawPct
        };

        if (isPopular) return $"🤝 {community.MostPredictedPct:0}% picked the same score";
        if (majorityPct >= 60) return $"👍 {majorityPct:0}% agree with your result";
        if (majorityPct <= 20) return $"🔥 Bold call — only {majorityPct:0}% back this result";
        return $"📊 {majorityPct:0}% of predictors agree";
    }
}
