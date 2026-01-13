using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Components;

public partial class CompetitionFilter
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;

    [Parameter] public EventCallback<CompetitionFilterState> OnStateChanged { get; set; }

    private List<CompetitionResult> _competitions = [];
    private List<(string Name, string? FlagUrl)> _countries = [];
    private List<CompetitionResult> _filteredCompetitions = [];
    private List<SeasonResult> _seasons = [];
    private string? _selectedCountry;
    private CompetitionResult? _selectedCompetition;
    private SeasonResult? _selectedSeason;
    private bool _loaded;

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
            _filteredCompetitions = _competitions.Where(c => c.CountryName == _selectedCountry).ToList();
            _selectedCompetition = _filteredCompetitions.FirstOrDefault(c => c.Id == defaultCompetition.Id);
            _loaded = true;
            StateHasChanged();
            await Task.Yield();
            if (_selectedCompetition is not null)
                await LoadSeasonsAsync(_selectedCompetition);
        }

        _loaded = true;
        StateHasChanged();
    }

    private async Task LoadSeasonsAsync(CompetitionResult competition)
    {
        var response = await Api.GetSeasonsAsync(competition.Code, CancellationToken.None);
        if (response is { Success: true, Data: not null })
            _seasons = response.Data;

        _selectedSeason = _seasons.FirstOrDefault(s => s.IsCurrent) ?? _seasons.FirstOrDefault();
        await NotifyState();
    }

    private async Task OnCountryChanged(string? country)
    {
        _selectedCountry = country;
        _selectedCompetition = null;
        _selectedSeason = null;
        _seasons = [];
        _filteredCompetitions = country is null ? [] : _competitions.Where(c => c.CountryName == country).ToList();
        if (_filteredCompetitions.Count == 1)
            await OnCompetitionChanged(_filteredCompetitions[0]);
        else
            await NotifyState();
    }

    private async Task OnCompetitionChanged(CompetitionResult? competition)
    {
        _selectedCompetition = competition;
        _seasons = [];
        _selectedSeason = null;
        if (competition is not null)
            await LoadSeasonsAsync(competition);
        else
            await NotifyState();
    }

    private async Task OnSeasonChanged(SeasonResult? season)
    {
        _selectedSeason = season;
        await NotifyState();
    }

    private async Task NotifyState() =>
        await OnStateChanged.InvokeAsync(new CompetitionFilterState(_selectedCompetition, _selectedSeason));
}

public record CompetitionFilterState(CompetitionResult? Competition, SeasonResult? Season);
