using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class LeagueTable
{
    private const string AppName = "LEAGUE TABLE";
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;

    private List<CompetitionResult> _competitions = [];
    private List<(string Name, string? FlagUrl)> _countries = [];
    private List<CompetitionResult> _filteredCompetitions = [];
    private List<SeasonResult> _seasons = [];
    private List<LeagueTableRow> _table = [];
    private List<CompetitionZoneResult> _zones = [];
    private string? _selectedCountry;
    private CompetitionResult? _selectedCompetition;
    private SeasonResult? _selectedSeason;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        var response = await Api.GetCompetitionsAsync(CancellationToken.None);
        if (response is { Success: true, Data: not null })
        {
            _competitions = response.Data;
            _countries = _competitions
                .Select(c => (c.CountryName, c.CountryFlagUrl))
                .DistinctBy(c => c.CountryName)
                .OrderBy(c => c.CountryName)
                .ToList();

            _filteredCompetitions = _competitions
                .Where(c => c.CountryName == CountryNames.England).ToList();
        }

        // render dropdowns with items first
        StateHasChanged();
        await Task.Yield();

        // now set selections so MudSelect can match against rendered items
        _selectedCountry = CountryNames.England;
        var pl = _filteredCompetitions.FirstOrDefault(c => c.Code == CompetitionCodes.PremierLeague);
        if (pl is not null)
        {
            _selectedCompetition = pl;
            StateHasChanged();
            await Task.Yield();
            await LoadCompetitionData(pl);
        }

        StateHasChanged();
    }

    private async Task LoadCompetitionData(CompetitionResult competition)
    {
        var seasonsTask = Api.GetSeasonsAsync(competition.Code, CancellationToken.None);
        var zonesTask = Api.GetCompetitionZonesAsync(competition.Code, CancellationToken.None);
        await Task.WhenAll(seasonsTask, zonesTask);

        var seasonsResponse = await seasonsTask;
        if (seasonsResponse is { Success: true, Data: not null })
            _seasons = seasonsResponse.Data;

        var zonesResponse = await zonesTask;
        if (zonesResponse is { Success: true, Data: not null })
            _zones = zonesResponse.Data;

        StateHasChanged();
        await Task.Yield();

        _selectedSeason = _seasons.FirstOrDefault(s => s.IsCurrent) ?? _seasons.FirstOrDefault();
        if (_selectedSeason is not null)
            await LoadTableAsync(_selectedSeason.Id);
    }

    private async Task OnCountryChanged(string? country)
    {
        _selectedCountry = country;
        _selectedCompetition = null;
        _selectedSeason = null;
        _seasons = [];
        _table = [];
        _zones = [];
        _filteredCompetitions = country is null
            ? []
            : _competitions.Where(c => c.CountryName == country).ToList();

        if (_filteredCompetitions.Count == 1)
            await OnCompetitionChanged(_filteredCompetitions[0]);
    }

    private async Task OnCompetitionChanged(CompetitionResult? competition)
    {
        _selectedCompetition = competition;
        _seasons = [];
        _selectedSeason = null;
        _table = [];
        _zones = [];

        if (competition is null) return;

        await Loading.While(async () => await LoadCompetitionData(competition));
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
