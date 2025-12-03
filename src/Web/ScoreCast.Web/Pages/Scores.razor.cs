using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Scores
{
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;

    private const string AppName = "SCORES";

    private List<CompetitionResult> _competitions = [];
    private List<string> _countries = [];
    private List<CompetitionResult> _filteredCompetitions = [];
    private List<SeasonResult> _seasons = [];
    private GameweekMatchesResult? _gameweek;
    private string? _selectedCountry;
    private CompetitionResult? _selectedCompetition;
    private SeasonResult? _selectedSeason;
    private readonly HashSet<long> _expandedMatches = [];
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
                _countries = _competitions.Select(c => c.CountryName).Distinct().OrderBy(c => c).ToList();
                _filteredCompetitions = _competitions.Where(c => c.CountryName == CountryNames.England).ToList();
            }

            StateHasChanged();
            await Task.Yield();

            _selectedCountry = CountryNames.England;
            var pl = _filteredCompetitions.FirstOrDefault(c => c.Code == CompetitionCodes.PremierLeague);
            if (pl is not null)
            {
                _selectedCompetition = pl;
                StateHasChanged();
                await Task.Yield();
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
            await LoadGameweekAsync(_selectedSeason.Id, 0);
    }

    private async Task OnCountryChanged(string? country)
    {
        _selectedCountry = country;
        _selectedCompetition = null;
        _selectedSeason = null;
        _seasons = [];
        _gameweek = null;
        _filteredCompetitions = country is null ? [] : _competitions.Where(c => c.CountryName == country).ToList();
        if (_filteredCompetitions.Count == 1)
            await OnCompetitionChanged(_filteredCompetitions[0]);
    }

    private async Task OnCompetitionChanged(CompetitionResult? competition)
    {
        _selectedCompetition = competition;
        _seasons = [];
        _selectedSeason = null;
        _gameweek = null;
        if (competition is not null)
            await Loading.While(async () => await LoadSeasonsAsync(competition));
    }

    private async Task OnSeasonChanged(SeasonResult? season)
    {
        _selectedSeason = season;
        _gameweek = null;
        if (season is not null)
            await LoadGameweekAsync(season.Id, 0);
    }

    private async Task LoadGameweekAsync(long seasonId, int gameweekNumber)
    {
        _expandedMatches.Clear();
        await Loading.While(async () =>
        {
            var response = await Api.GetGameweekMatchesAsync(seasonId, gameweekNumber, CancellationToken.None);
            if (response is { Success: true, Data: not null })
                _gameweek = response.Data;
            else
                Alert.Add("Failed to load matches", Severity.Error);
        });
    }

    private async Task PreviousGameweek()
    {
        if (_selectedSeason is not null && _gameweek is not null && _gameweek.GameweekNumber > 1)
            await LoadGameweekAsync(_selectedSeason.Id, _gameweek.GameweekNumber - 1);
    }

    private async Task NextGameweek()
    {
        if (_selectedSeason is not null && _gameweek is not null && _gameweek.GameweekNumber < _gameweek.TotalGameweeks)
            await LoadGameweekAsync(_selectedSeason.Id, _gameweek.GameweekNumber + 1);
    }

    private void ToggleMatch(long matchId)
    {
        if (!_expandedMatches.Remove(matchId))
            _expandedMatches.Add(matchId);
    }

    private static string FormatEvent(MatchEventDetail e) => e.EventType switch
    {
        EventTypes.Goal => e.Value > 1 ? $"⚽ x{e.Value}" : "⚽",
        EventTypes.PenaltyGoal => "⚽ (P)",
        EventTypes.OwnGoal => "⚽ (OG)",
        EventTypes.Assist => "🅰️",
        EventTypes.YellowCard => "🟨",
        EventTypes.RedCard => "🟥",
        EventTypes.PenaltySaved => "🧤",
        EventTypes.PenaltyMissed => "❌",
        _ => ""
    };

    private record DisplayLine(string Text, string? Minute, double SortKey, bool Bold);

    private static List<DisplayLine> GetDisplayLines(List<MatchEventDetail> events, bool isHome)
    {
        var lines = new List<DisplayLine>();

        foreach (var e in events.Where(e => e.IsHome == isHome && e.EventType is not EventTypes.SubIn and not EventTypes.SubOut))
        {
            var isGoal = e.EventType is EventTypes.Goal or EventTypes.PenaltyGoal or EventTypes.OwnGoal;
            lines.Add(new DisplayLine(
                isHome ? $"{e.PlayerName} {FormatEvent(e)}" : $"{FormatEvent(e)} {e.PlayerName}",
                e.Minute, ParseMinute(e.Minute), isGoal));
        }

        foreach (var s in GetSubPairs(events, isHome))
            lines.Add(new DisplayLine(
                $"🔼 {s.PlayerOn} 🔽 {s.PlayerOff}",
                s.Minute, ParseMinute(s.Minute), false));

        return lines.OrderBy(l => l.SortKey).ToList();
    }

    private static double ParseMinute(string? minute)
    {
        if (minute is null) return 999;
        var clean = minute.Replace("'", "").Replace(" ", "");
        var parts = clean.Split('+');
        if (double.TryParse(parts[0], out var main))
            return parts.Length > 1 && double.TryParse(parts[1], out var added) ? main + added * 0.01 : main;
        return 999;
    }

    private record SubPair(string PlayerOn, string PlayerOff, string? Minute);

    private static List<SubPair> GetSubPairs(List<MatchEventDetail> events, bool isHome)
    {
        var subs = events.Where(e => e.IsHome == isHome).ToList();
        var subIns = subs.Where(e => e.EventType == EventTypes.SubIn).ToList();
        var subOffs = subs.Where(e => e.EventType == EventTypes.SubOut).ToList();

        return subIns.Select(si =>
        {
            var off = subOffs.FirstOrDefault(so => so.Minute == si.Minute);
            if (off is not null) subOffs.Remove(off);
            return new SubPair(si.PlayerName, off?.PlayerName ?? "", si.Minute);
        }).ToList();
    }
}
