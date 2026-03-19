using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Insights : ScoreCastComponentBase
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private List<CompetitionResult> _competitions = [];
    private CompetitionResult? _selectedCompetition;
    private List<MatchInsightResult> _insights = [];
    private int _gameweekNumber;
    private bool _loaded;

    protected override async Task OnInitializedAsync()
    {
        var compsTask = Api.GetCompetitionsAsync(default);
        var defaultTask = Api.GetDefaultCompetitionAsync(default);
        await Task.WhenAll(compsTask, defaultTask);

        if (compsTask.Result is { Success: true, Data: not null })
            _competitions = compsTask.Result.Data;

        var defaultCode = defaultTask.Result is { Success: true, Data: not null }
            ? defaultTask.Result.Data.Code : null;
        _selectedCompetition = _competitions.FirstOrDefault(c => c.Code == defaultCode) ?? _competitions.FirstOrDefault();

        await LoadInsights();
    }

    private async Task OnCompetitionChanged(CompetitionResult comp)
    {
        _selectedCompetition = comp;
        _loaded = false;
        _insights = [];
        StateHasChanged();
        await LoadInsights();
        StateHasChanged();
    }

    private async Task LoadInsights()
    {
        if (_selectedCompetition is null) { _loaded = true; return; }

        var seasons = await Api.GetSeasonsAsync(_selectedCompetition.Code, default);
        var current = seasons.Data?.FirstOrDefault(s => s.IsCurrent);
        if (current is null) { _loaded = true; return; }

        var gw = await Api.GetGameweekMatchesAsync(current.Id, 0, default);
        if (gw is not { Success: true, Data: not null }) { _loaded = true; return; }

        var now = ScoreCastDateTime.Now.Value;
        var excludedIds = gw.Data.Matches
            .Where(m => m.Status is nameof(MatchStatus.Postponed) or nameof(MatchStatus.Live) or nameof(MatchStatus.Finished))
            .Select(m => m.MatchId).ToHashSet();

        List<MatchInsightResult>? Filter(List<MatchInsightResult>? list) =>
            list?.Where(m => m.KickoffTime.HasValue && m.KickoffTime.Value > now && !excludedIds.Contains(m.MatchId))
                .ToList() is { Count: > 0 } f ? f : null;

        var resp = await Api.GetMatchInsightsAsync(current.Id, gw.Data.CurrentGameweek, default);
        var filtered = resp is { Success: true, Data: not null } ? Filter(resp.Data) : null;
        if (filtered is not null)
        {
            _insights = filtered;
            _gameweekNumber = gw.Data.CurrentGameweek;
        }
        else
        {
            var next = await Api.GetMatchInsightsAsync(current.Id, gw.Data.CurrentGameweek + 1, default);
            var nextFiltered = next is { Success: true, Data: not null } ? Filter(next.Data) : null;
            _insights = nextFiltered ?? [];
            _gameweekNumber = nextFiltered is not null ? gw.Data.CurrentGameweek + 1 : gw.Data.CurrentGameweek;
        }

        _loaded = true;
    }
}
