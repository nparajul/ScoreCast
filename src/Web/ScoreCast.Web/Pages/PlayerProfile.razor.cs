using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;
using ScoreCast.Web.ViewModels;

namespace ScoreCast.Web.Pages;

public partial class PlayerProfile : ScoreCastComponentBase
{
    [Parameter] public long LeagueId { get; set; }
    [Parameter] public long UserId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private IClientTimeProvider ClientTime { get; set; } = null!;

    private PlayerProfileResult? _profile;
    private GameweekMatchesResult? _gameweek;
    private List<PredictionMatchViewModel> _matches = [];
    private List<ScoringRuleResult> _scoringRules = [];
    private List<RiskPlayViewModel> _riskPlays = [];
    private bool _predictionsVisible;
    private bool _riskPlaysVisible;
    private long _seasonId;
    private int _startingGwNumber = 1;
    private string? _competitionName;
    private string? _competitionLogoUrl;
    private string? _leagueName;
    private bool _showBreakdown;
    private readonly HashSet<string> _expandedRules = [];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await ClientTime.InitializeAsync();

        await Loading.While(async () =>
        {
            var standingsResponse = await Api.GetLeagueStandingsAsync(LeagueId, CancellationToken.None);
            if (standingsResponse is not { Success: true, Data: not null }) { Nav.NavigateTo($"/dashboard/{LeagueId}"); return; }
            _seasonId = standingsResponse.Data.SeasonId;
            _startingGwNumber = standingsResponse.Data.StartingGameweekNumber ?? 1;
            _competitionName = standingsResponse.Data.CompetitionName;
            _competitionLogoUrl = standingsResponse.Data.CompetitionLogoUrl;
            _leagueName = standingsResponse.Data.LeagueName;

            var profileTask = Api.GetPlayerProfileAsync(UserId, LeagueId, CancellationToken.None);
            var rulesTask = Api.GetScoringRulesAsync(CancellationToken.None);
            await Task.WhenAll(profileTask, rulesTask);

            if (profileTask.Result is { Success: true, Data: not null })
                _profile = profileTask.Result.Data;
            else { Alert.Add(profileTask.Result?.Message ?? "Player not found", Severity.Error); return; }

            if (rulesTask.Result is { Success: true, Data: not null })
                _scoringRules = rulesTask.Result.Data;

            await LoadGameweek(SharedConstants.CurrentGameweek);
        });
        StateHasChanged();
    }

    private async Task LoadGameweek(int gameweekNumber)
    {
        var matchesTask = Api.GetGameweekMatchesAsync(_seasonId, gameweekNumber, CancellationToken.None);
        await matchesTask;

        if (matchesTask.Result is not { Success: true, Data: not null }) return;
        _gameweek = matchesTask.Result.Data;
        _matches = _gameweek.Matches.Select(PredictionMatchViewModel.FromMatch).ToList();

        var gwResult = await Api.GetPlayerGameweekAsync(UserId, LeagueId, _seasonId, _gameweek.GameweekId, CancellationToken.None);
        _predictionsVisible = false;
        _riskPlaysVisible = false;
        _riskPlays = [];

        if (gwResult is { Success: true, Data: not null })
        {
            _predictionsVisible = gwResult.Data.PredictionsVisible;
            _riskPlaysVisible = gwResult.Data.RiskPlaysVisible;

            if (_predictionsVisible)
            {
                foreach (var prediction in gwResult.Data.Predictions)
                {
                    var match = _matches.FirstOrDefault(m => m.MatchId == prediction.MatchId);
                    if (match is null) continue;
                    match.PredictedHomeScore = prediction.PredictedHomeScore;
                    match.PredictedAwayScore = prediction.PredictedAwayScore;
                    match.Outcome = prediction.Outcome?.ToString();
                    match.HasSavedPrediction = true;
                }
            }

            if (_riskPlaysVisible)
            {
                _riskPlays = gwResult.Data.RiskPlays.Select(r =>
                {
                    var vm = new RiskPlayViewModel { RiskType = r.RiskType, MatchId = r.MatchId, Selection = r.Selection, BonusPoints = r.BonusPoints, IsWon = r.IsWon, IsResolved = r.IsResolved };
                    return vm;
                }).ToList();
            }
        }
    }

    private async Task PreviousGameweek()
    {
        if (_gameweek is null || _gameweek.GameweekNumber <= _startingGwNumber) return;
        await Loading.While(async () => await LoadGameweek(_gameweek.GameweekNumber - 1));
        StateHasChanged();
    }

    private async Task NextGameweek()
    {
        if (_gameweek is null || _gameweek.GameweekNumber >= _gameweek.TotalGameweeks) return;
        await Loading.While(async () => await LoadGameweek(_gameweek.GameweekNumber + 1));
        StateHasChanged();
    }

    private void ToggleRule(string label)
    {
        if (!_expandedRules.Remove(label)) _expandedRules.Add(label);
    }

    private string MatchLabel(long matchId) => _matches.FirstOrDefault(x => x.MatchId == matchId) is { } m ? $"{m.HomeTeamShortName} vs {m.AwayTeamShortName}" : "";
}
