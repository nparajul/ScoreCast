using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class PointsTable
{
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;

    private List<CompetitionResult> _competitions = [];
    private List<(string Name, string? FlagUrl)> _countries = [];
    private List<CompetitionResult> _filteredCompetitions = [];
    private List<SeasonResult> _seasons = [];
    private PointsTableResult? _result;
    private BracketResult? _bracket;
    private List<CompetitionZoneResult> _zones = [];
    private string? _selectedCountry;
    private CompetitionResult? _selectedCompetition;
    private SeasonResult? _selectedSeason;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        var competitionsTask = Api.GetCompetitionsAsync(CancellationToken.None);
        var defaultTask = Api.GetDefaultCompetitionAsync(CancellationToken.None);
        await Task.WhenAll(competitionsTask, defaultTask);

        var response = await competitionsTask;
        if (response is { Success: true, Data: not null })
        {
            _competitions = response.Data;
            _countries = _competitions
                .Select(c => (c.CountryName, c.CountryFlagUrl))
                .DistinctBy(c => c.CountryName)
                .OrderBy(c => c.CountryName)
                .ToList();
        }

        var defaultResponse = await defaultTask;
        var defaultCompetition = defaultResponse is { Success: true, Data: not null }
            ? _competitions.FirstOrDefault(c => c.Code == defaultResponse.Data.Code)
            : _competitions.FirstOrDefault(c => c.Code == CompetitionCodes.PremierLeague);

        if (defaultCompetition is not null)
        {
            _selectedCountry = defaultCompetition.CountryName;
            _filteredCompetitions = _competitions
                .Where(c => c.CountryName == _selectedCountry).ToList();
            _selectedCompetition = _filteredCompetitions.FirstOrDefault(c => c.Id == defaultCompetition.Id);
            StateHasChanged();
            await Task.Yield();
            await LoadCompetitionData(defaultCompetition);
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
        _result = null;
        _bracket = null;
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
        _result = null;
        _bracket = null;
        _zones = [];

        if (competition is null) return;

        await Loading.While(async () => await LoadCompetitionData(competition));
    }

    private async Task OnSeasonChanged(SeasonResult? season)
    {
        _selectedSeason = season;
        _result = null;
        _bracket = null;
        if (season is not null)
            await LoadTableAsync(season.Id);
    }

    private async Task LoadTableAsync(long seasonId)
    {
        await Loading.While(async () =>
        {
            var tableTask = Api.GetPointsTableAsync(seasonId, CancellationToken.None);
            var bracketTask = Api.GetBracketAsync(seasonId, CancellationToken.None);
            await Task.WhenAll(tableTask, bracketTask);

            var response = await tableTask;
            if (response is { Success: true, Data: not null })
                _result = response.Data;
            else
                Alert.Add("Failed to load points table", Severity.Error);

            var bracketResponse = await bracketTask;
            if (bracketResponse is { Success: true, Data: not null })
                _bracket = bracketResponse.Data;
        });
    }

    private bool IsGroupFormat => _result?.Format is CompetitionFormat.GroupAndKnockout;
}
