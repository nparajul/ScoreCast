using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class LeagueTable
{
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;

    private List<CompetitionResult> _competitions = [];
    private List<SeasonResult> _seasons = [];
    private List<LeagueTableRow> _table = [];
    private List<CompetitionZoneResult> _zones = [];
    private CompetitionResult? _selectedCompetition;
    private SeasonResult? _selectedSeason;
    private bool _loaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender || _loaded) return;
        _loaded = true;
        await InvokeAsync(async () =>
        {
            var response = await Api.GetCompetitionsAsync(CancellationToken.None);
            if (response is { Success: true, Data: not null })
            {
                _competitions = response.Data;
                var pl = _competitions.FirstOrDefault(c => c.Code == CompetitionCodes.PremierLeague);
                if (pl is not null)
                    await OnCompetitionChanged(pl);
            }
            StateHasChanged();
        });
    }

    private Task<IEnumerable<CompetitionResult>> SearchCompetitions(string? value, CancellationToken ct)
    {
        var results = string.IsNullOrWhiteSpace(value)
            ? _competitions
            : _competitions.Where(c => c.Name.Contains(value, StringComparison.OrdinalIgnoreCase)
                                    || c.Code.Contains(value, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(results);
    }

    private async Task OnCompetitionChanged(CompetitionResult? competition)
    {
        _selectedCompetition = competition;
        _seasons = [];
        _selectedSeason = null;
        _table = [];
        _zones = [];

        if (competition is null) return;

        await Loading.While(async () =>
        {
            var seasonsTask = Api.GetSeasonsAsync(competition.Code, CancellationToken.None);
            var zonesTask = Api.GetCompetitionZonesAsync(competition.Code, CancellationToken.None);
            await Task.WhenAll(seasonsTask, zonesTask);

            var seasonsResponse = await seasonsTask;
            if (seasonsResponse is { Success: true, Data: not null })
            {
                _seasons = seasonsResponse.Data;
                _selectedSeason = _seasons.FirstOrDefault(s => s.IsCurrent) ?? _seasons.FirstOrDefault();
            }

            var zonesResponse = await zonesTask;
            if (zonesResponse is { Success: true, Data: not null })
                _zones = zonesResponse.Data;
        });

        if (_selectedSeason is not null)
            await LoadTableAsync(_selectedSeason.Id);
    }

    private async Task OnSeasonChanged(SeasonResult? season)
    {
        _selectedSeason = season;
        _table = [];
        if (season is not null)
            await LoadTableAsync(season.Id);
    }

    private async Task LoadTableAsync(long seasonId)
    {
        await Loading.While(async () =>
        {
            var response = await Api.GetLeagueTableAsync(seasonId, CancellationToken.None);
            if (response is { Success: true, Data: not null })
                _table = response.Data;
            else
                Alert.Add("Failed to load league table", Severity.Error);
        });
    }

    private string RowStyleFunc(LeagueTableRow row, int _)
    {
        var zone = _zones.FirstOrDefault(z => row.Position >= z.StartPosition && row.Position <= z.EndPosition);
        if (zone is null) return string.Empty;
        return $"background:{zone.Color}15;border-left:3px solid {zone.Color};";
    }
}
