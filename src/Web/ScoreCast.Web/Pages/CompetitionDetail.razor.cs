using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class CompetitionDetail : ScoreCastComponentBase
{
    [Parameter] public long CompetitionId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private IClientTimeProvider ClientTime { get; set; } = null!;

    private CompetitionResult? _competition;
    private List<SeasonResult> _seasons = [];
    private SeasonResult? _selectedSeason;
    private PointsTableResult? _table;
    private List<CompetitionZoneResult> _zones = [];
    private GameweekMatchesResult? _gameweek;
    private PlayerStatsResult? _playerStats;
    private readonly HashSet<long> _expandedMatches = [];

    private string _activeTab = "Table";
    private string _playerStatTab = "Goals";
    private static readonly string[] _tabs = ["Table", "Scores", "Players"];

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await ClientTime.InitializeAsync();

        await Loading.While(async () =>
        {
            var comps = await Api.GetCompetitionsAsync(CancellationToken.None);
            _competition = comps?.Data?.FirstOrDefault(c => c.Id == CompetitionId);
            if (_competition is null) return;

            var seasons = await Api.GetSeasonsAsync(_competition.Code, CancellationToken.None);
            if (seasons is { Success: true, Data: not null })
            {
                _seasons = seasons.Data;
                _selectedSeason = _seasons.FirstOrDefault(s => s.IsCurrent) ?? _seasons.FirstOrDefault();
            }

            if (_selectedSeason is not null)
                await LoadSeasonDataAsync();
        });

        StateHasChanged();
    }

    private async Task LoadSeasonDataAsync()
    {
        if (_selectedSeason is null || _competition is null) return;

        var tableTask = Api.GetPointsTableAsync(_selectedSeason.Id, CancellationToken.None);
        var zonesTask = Api.GetCompetitionZonesAsync(_competition.Code, CancellationToken.None);
        var gwTask = Api.GetGameweekMatchesAsync(_selectedSeason.Id, SharedConstants.CurrentGameweek, CancellationToken.None);
        var statsTask = Api.GetPlayerStatsAsync(_selectedSeason.Id, CancellationToken.None);

        await Task.WhenAll(tableTask, zonesTask, gwTask, statsTask);

        if (tableTask.Result is { Success: true, Data: not null }) _table = tableTask.Result.Data;
        if (zonesTask.Result is { Success: true, Data: not null }) _zones = zonesTask.Result.Data;
        if (gwTask.Result is { Success: true, Data: not null }) _gameweek = gwTask.Result.Data;
        if (statsTask.Result is { Success: true, Data: not null }) _playerStats = statsTask.Result.Data;
    }

    private async Task OnSeasonChanged(ChangeEventArgs e)
    {
        if (!long.TryParse(e.Value?.ToString(), out var seasonId)) return;
        _selectedSeason = _seasons.FirstOrDefault(s => s.Id == seasonId);
        _table = null; _gameweek = null; _playerStats = null; _zones = [];

        await Loading.While(LoadSeasonDataAsync);
        StateHasChanged();
    }

    private void SwitchTab(string tab) => _activeTab = tab;

    private async Task PrevGw()
    {
        if (_selectedSeason is null || _gameweek is null || _gameweek.GameweekNumber <= 1) return;
        await Loading.While(async () =>
        {
            var r = await Api.GetGameweekMatchesAsync(_selectedSeason.Id, _gameweek.GameweekNumber - 1, CancellationToken.None);
            if (r is { Success: true, Data: not null }) _gameweek = r.Data;
        });
        _expandedMatches.Clear();
    }

    private async Task NextGw()
    {
        if (_selectedSeason is null || _gameweek is null || _gameweek.GameweekNumber >= _gameweek.TotalGameweeks) return;
        await Loading.While(async () =>
        {
            var r = await Api.GetGameweekMatchesAsync(_selectedSeason.Id, _gameweek.GameweekNumber + 1, CancellationToken.None);
            if (r is { Success: true, Data: not null }) _gameweek = r.Data;
        });
        _expandedMatches.Clear();
    }

    private void ToggleMatch(long matchId)
    {
        if (!_expandedMatches.Remove(matchId)) _expandedMatches.Add(matchId);
    }

    private record DateGroup(string Label, List<MatchDetail> Matches);

    private List<DateGroup> GetMatchesByDate()
    {
        if (_gameweek is null) return [];
        var today = ClientTime.Today;
        return _gameweek.Matches
            .GroupBy(m => m.KickoffTime.HasValue ? DateOnly.FromDateTime(ClientTime.ToLocal(m.KickoffTime.Value)) : (DateOnly?)null)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var label = g.Key switch
                {
                    null => "TBD",
                    var d when d == today => "Today",
                    var d when d == today.AddDays(1) => "Tomorrow",
                    var d when d == today.AddDays(-1) => "Yesterday",
                    var d => d.Value.ToString("dddd, MMMM d")
                };
                return new DateGroup(label, g.ToList());
            })
            .ToList();
    }

    private record PlayerStatDisplay(string PlayerName, string? TeamLogo, int Value);

    private List<PlayerStatDisplay> GetPlayerStatRows()
    {
        if (_playerStats is null) return [];
        return _playerStatTab switch
        {
            "Goals" => _playerStats.Rows.OrderByDescending(p => p.Goals).Take(20).Select(p => new PlayerStatDisplay(p.PlayerName, p.TeamLogo, p.Goals)).ToList(),
            "Assists" => _playerStats.Rows.OrderByDescending(p => p.Assists).Take(20).Select(p => new PlayerStatDisplay(p.PlayerName, p.TeamLogo, p.Assists)).ToList(),
            "Clean Sheets" => _playerStats.Rows.Where(p => p.CleanSheets > 0).OrderByDescending(p => p.CleanSheets).Take(20).Select(p => new PlayerStatDisplay(p.PlayerName, p.TeamLogo, p.CleanSheets)).ToList(),
            _ => []
        };
    }
}
