using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class PlayerStats
{
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;

    private List<CompetitionResult> _competitions = [];
    private List<SeasonResult> _seasons = [];
    private CompetitionResult? _selectedCompetition;
    private SeasonResult? _selectedSeason;
    private List<PlayerStatRow> _rows = [];
    private string _sortColumn = "Goals";
    private bool _sortDescending = true;
    private string _search = "";
    private bool _loaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _loaded) return;
        _loaded = true;
        await InvokeAsync(async () =>
        {
            var response = await Api.GetCompetitionsAsync(CancellationToken.None);
            if (response is { Success: true, Data: not null })
                _competitions = response.Data;

            StateHasChanged();
            await Task.Yield();

            var pl = _competitions.FirstOrDefault(c => c.Code == CompetitionCodes.PremierLeague);
            if (pl is not null)
            {
                _selectedCompetition = pl;
                await LoadSeasonsAsync(pl);
            }

            StateHasChanged();
        });
    }

    private async Task LoadSeasonsAsync(CompetitionResult competition)
    {
        var response = await Api.GetSeasonsAsync(competition.Code, CancellationToken.None);
        if (response is { Success: true, Data: not null })
            _seasons = response.Data;

        StateHasChanged();
        await Task.Yield();

        _selectedSeason = _seasons.FirstOrDefault(s => s.IsCurrent) ?? _seasons.FirstOrDefault();
        if (_selectedSeason is not null)
            await LoadStatsAsync(_selectedSeason.Id);
    }

    private async Task OnCompetitionChanged(CompetitionResult? competition)
    {
        _selectedCompetition = competition;
        _seasons = [];
        _selectedSeason = null;
        _rows = [];
        if (competition is not null)
            await Loading.While(async () => await LoadSeasonsAsync(competition));
    }

    private async Task OnSeasonChanged(SeasonResult? season)
    {
        _selectedSeason = season;
        _rows = [];
        if (season is not null)
            await LoadStatsAsync(season.Id);
    }

    private async Task LoadStatsAsync(long seasonId)
    {
        await Loading.While(async () =>
        {
            var response = await Api.GetPlayerStatsAsync(seasonId, CancellationToken.None);
            if (response is { Success: true, Data: not null })
                _rows = response.Data.Rows;
            else
                Alert.Add("Failed to load player stats", Severity.Error);
        });
    }

    private void SortBy(string column)
    {
        if (_sortColumn == column)
            _sortDescending = !_sortDescending;
        else
        {
            _sortColumn = column;
            _sortDescending = true;
        }
    }

    private IEnumerable<PlayerStatRow> SortedRows
    {
        get
        {
            var filtered = string.IsNullOrWhiteSpace(_search)
                ? _rows
                : _rows.Where(r => r.PlayerName.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || (r.TeamName?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            return _sortColumn switch
            {
                "Goals" => _sortDescending ? filtered.OrderByDescending(r => r.Goals + r.PenaltyGoals) : filtered.OrderBy(r => r.Goals + r.PenaltyGoals),
                "Assists" => _sortDescending ? filtered.OrderByDescending(r => r.Assists) : filtered.OrderBy(r => r.Assists),
                "YellowCards" => _sortDescending ? filtered.OrderByDescending(r => r.YellowCards) : filtered.OrderBy(r => r.YellowCards),
                "RedCards" => _sortDescending ? filtered.OrderByDescending(r => r.RedCards) : filtered.OrderBy(r => r.RedCards),
                _ => filtered.OrderByDescending(r => r.Goals + r.PenaltyGoals)
            };
        }
    }

    private string SortIcon(string column) =>
        _sortColumn != column ? "" : _sortDescending ? Icons.Material.Filled.ArrowDownward : Icons.Material.Filled.ArrowUpward;
}
