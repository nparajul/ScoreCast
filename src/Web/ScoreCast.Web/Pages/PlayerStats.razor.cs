using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class PlayerStats
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;

    private SeasonResult? _selectedSeason;
    private List<PlayerStatRow> _rows = [];
    private string _sortColumn = "Goals";
    private bool _sortDescending = true;
    private string _search = "";
    private string _mobileTab = "Overall";

    private async Task OnFilterChanged(CompetitionFilterState state)
    {
        _selectedSeason = state.Season;
        _rows = [];
        if (state.Season is not null)
            await LoadStatsAsync(state.Season.Id);
        StateHasChanged();
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
                "Goals" => _sortDescending
                    ? filtered.OrderByDescending(r => r.Goals + r.PenaltyGoals).ThenByDescending(r => r.Assists)
                    : filtered.OrderBy(r => r.Goals + r.PenaltyGoals).ThenBy(r => r.Assists),
                "Assists" => _sortDescending
                    ? filtered.OrderByDescending(r => r.Assists).ThenByDescending(r => r.Goals + r.PenaltyGoals)
                    : filtered.OrderBy(r => r.Assists).ThenBy(r => r.Goals + r.PenaltyGoals),
                "YellowCards" => _sortDescending
                    ? filtered.OrderByDescending(r => r.YellowCards).ThenByDescending(r => r.RedCards)
                    : filtered.OrderBy(r => r.YellowCards).ThenBy(r => r.RedCards),
                "RedCards" => _sortDescending
                    ? filtered.OrderByDescending(r => r.RedCards).ThenByDescending(r => r.YellowCards)
                    : filtered.OrderBy(r => r.RedCards).ThenBy(r => r.YellowCards),
                _ => filtered.OrderByDescending(r => r.Goals + r.PenaltyGoals).ThenByDescending(r => r.Assists)
            };
        }
    }

    private string SortIcon(string column) =>
        _sortColumn != column ? "" : _sortDescending ? Icons.Material.Filled.ArrowDownward : Icons.Material.Filled.ArrowUpward;

    private List<PlayerStatRow> CleanSheetRows
    {
        get
        {
            var filtered = string.IsNullOrWhiteSpace(_search)
                ? _rows
                : _rows.Where(r => r.PlayerName.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || (r.TeamName?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false));
            return filtered.Where(r => r.CleanSheets > 0).OrderByDescending(r => r.CleanSheets).ToList();
        }
    }

    private IEnumerable<PlayerStatRow> MobileTabRows
    {
        get
        {
            var filtered = string.IsNullOrWhiteSpace(_search)
                ? _rows
                : _rows.Where(r => r.PlayerName.Contains(_search, StringComparison.OrdinalIgnoreCase)
                    || (r.TeamName?.Contains(_search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();

            return _mobileTab switch
            {
                "Goals" => filtered.OrderByDescending(r => r.Goals + r.PenaltyGoals).ThenByDescending(r => r.Assists),
                "Assists" => filtered.OrderByDescending(r => r.Assists).ThenByDescending(r => r.Goals + r.PenaltyGoals),
                "Clean Sheets" => filtered.Where(r => r.Position == ScoreCast.Shared.Constants.PlayerPositions.Goalkeeper)
                    .OrderByDescending(r => r.CleanSheets),
                "Discipline" => filtered.OrderByDescending(r => r.YellowCards + r.RedCards).ThenByDescending(r => r.RedCards),
                _ => filtered.OrderByDescending(r => r.Goals + r.PenaltyGoals).ThenByDescending(r => r.Assists)
            };
        }
    }
}
