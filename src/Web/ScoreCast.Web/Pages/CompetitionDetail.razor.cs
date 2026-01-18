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
    private string _playerStatTab = "Overall";
    private string _sortColumn = "Goals";
    private bool _sortDescending = true;
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

    private void SortBy(string column)
    {
        if (_sortColumn == column) _sortDescending = !_sortDescending;
        else { _sortColumn = column; _sortDescending = true; }
    }

    private string SortIcon(string column) =>
        _sortColumn != column ? "" : _sortDescending ? Icons.Material.Filled.ArrowDownward : Icons.Material.Filled.ArrowUpward;

    private IEnumerable<PlayerStatRow> SortedPlayerRows
    {
        get
        {
            if (_playerStats is null) return [];
            var rows = _playerStats.Rows.AsEnumerable();
            return _sortColumn switch
            {
                "Goals" => _sortDescending ? rows.OrderByDescending(r => r.Goals + r.PenaltyGoals).ThenByDescending(r => r.Assists) : rows.OrderBy(r => r.Goals + r.PenaltyGoals),
                "Assists" => _sortDescending ? rows.OrderByDescending(r => r.Assists).ThenByDescending(r => r.Goals + r.PenaltyGoals) : rows.OrderBy(r => r.Assists),
                "YellowCards" => _sortDescending ? rows.OrderByDescending(r => r.YellowCards).ThenByDescending(r => r.RedCards) : rows.OrderBy(r => r.YellowCards),
                "RedCards" => _sortDescending ? rows.OrderByDescending(r => r.RedCards).ThenByDescending(r => r.YellowCards) : rows.OrderBy(r => r.RedCards),
                _ => rows.OrderByDescending(r => r.Goals + r.PenaltyGoals)
            };
        }
    }

    private IEnumerable<PlayerStatRow> MobileTabRows
    {
        get
        {
            if (_playerStats is null) return [];
            var rows = _playerStats.Rows.AsEnumerable();
            return _playerStatTab switch
            {
                "Goals" => rows.OrderByDescending(r => r.Goals + r.PenaltyGoals).ThenByDescending(r => r.Assists),
                "Assists" => rows.OrderByDescending(r => r.Assists).ThenByDescending(r => r.Goals + r.PenaltyGoals),
                "Discipline" => rows.OrderByDescending(r => r.YellowCards + r.RedCards).ThenByDescending(r => r.RedCards),
                _ => rows.OrderByDescending(r => r.Goals + r.PenaltyGoals).ThenByDescending(r => r.Assists)
            };
        }
    }
}
