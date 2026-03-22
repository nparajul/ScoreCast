using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;

namespace ScoreCast.Web.Components.Shared;

public partial class GlobalSearch
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    [Parameter] public bool Visible { get; set; }
    [Parameter] public EventCallback OnClose { get; set; }
    [Parameter] public bool Inline { get; set; }

    private string? _query;
    private List<SearchItem> _allItems = [];
    private List<SearchItem> _filtered = [];
    private List<SearchItem> _trending = [];
    private bool _dataLoaded;
    private bool _loading;

    private bool HasResults => _loading || _filtered.Count > 0 || _trending.Count > 0
                               || (_query?.Length >= 2);

    protected override async Task OnParametersSetAsync()
    {
        if (Visible && !_dataLoaded && _allItems.Count == 0)
        {
            _loading = true;
            StateHasChanged();
            await LoadDataAsync();
            _loading = false;
            StateHasChanged();
        }
    }

    private async Task OnSearchChanged(string? value)
    {
        _query = value;

        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            _filtered = [];
            return;
        }

        if (!_dataLoaded)
        {
            _loading = true;
            StateHasChanged();
            await LoadDataAsync();
            _loading = false;
        }

        _filtered = _allItems
            .Where(i => i.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .Take(15)
            .ToList();
    }

    private async Task LoadDataAsync()
    {
        if (_dataLoaded) return;

        var comps = await Api.GetCompetitionsAsync(CancellationToken.None);
        if (comps is not { Success: true, Data: not null }) { _dataLoaded = true; return; }

        // Build searchable items
        foreach (var c in comps.Data)
            _allItems.Add(new("🏆", c.Name, c.LogoUrl, "Competition", $"/competitions/{c.Id}"));

        var teamTasks = comps.Data.Select(async comp =>
        {
            var teams = await Api.GetTeamsAsync(comp.Name, CancellationToken.None);
            return (comp, teams);
        });
        var teamResults = await Task.WhenAll(teamTasks);

        foreach (var (comp, teams) in teamResults)
        {
            if (teams is not { Success: true, Data: not null }) continue;
            foreach (var t in teams.Data)
            {
                if (_allItems.All(i => i.Url != $"/teams/{t.Id}"))
                    _allItems.Add(new("🛡️", t.Name, t.LogoUrl, "Team", $"/teams/{t.Id}"));
            }
        }

        // Build trending: live/today matches → teams playing now
        await BuildTrending(comps.Data);
        _dataLoaded = true;
    }

    private async Task BuildTrending(List<CompetitionResult> comps)
    {
        var defaultResp = await Api.GetDefaultCompetitionAsync(CancellationToken.None);
        var defaultCode = defaultResp is { Success: true, Data: not null } ? defaultResp.Data.Code : "PL";
        var defaultComp = comps.FirstOrDefault(c => c.Code == defaultCode) ?? comps.FirstOrDefault();
        if (defaultComp is null) return;

        var seasons = await Api.GetSeasonsAsync(defaultComp.Code, CancellationToken.None);
        var current = seasons?.Data?.FirstOrDefault(s => s.IsCurrent);
        if (current is null) return;

        var gw = await Api.GetGameweekMatchesAsync(current.Id, SharedConstants.CurrentGameweek, CancellationToken.None);
        if (gw is not { Success: true, Data: not null }) return;

        var allMatches = gw.Data.Matches;

        // Also fetch next GW if current has no upcoming
        var hasUpcoming = allMatches.Any(m => m.Status == nameof(MatchStatus.Scheduled));
        if (!hasUpcoming && gw.Data.GameweekNumber < gw.Data.TotalGameweeks)
        {
            var nextGw = await Api.GetGameweekMatchesAsync(current.Id, gw.Data.GameweekNumber + 1, CancellationToken.None);
            if (nextGw is { Success: true, Data: not null })
                allMatches = allMatches.Concat(nextGw.Data.Matches).ToList();
        }

        // Live matches
        var live = allMatches.Where(m => m.Status == nameof(MatchStatus.Live)).ToList();
        foreach (var m in live)
        {
            _trending.Add(new("🔴", $"{m.HomeTeamShortName} vs {m.AwayTeamShortName} ({m.HomeScore}-{m.AwayScore})",
                null, "🔴 Live Now", $"/matches/{m.MatchId}"));
        }

        // Today's upcoming
        var today = ScoreCastDateTime.Now.Value.Date;
        var upcoming = allMatches
            .Where(m => m.Status == nameof(MatchStatus.Scheduled) && m.KickoffTime.HasValue
                        && m.KickoffTime.Value.Date == today)
            .OrderBy(m => m.KickoffTime).Take(4).ToList();
        foreach (var m in upcoming)
        {
            _trending.Add(new("⚽", $"{m.HomeTeamShortName} vs {m.AwayTeamShortName}",
                null, "⚽ Today's Matches", $"/matches/{m.MatchId}"));
        }

        // Next upcoming if nothing live or today
        if (live.Count == 0 && upcoming.Count == 0)
        {
            var next = allMatches
                .Where(m => m.Status == nameof(MatchStatus.Scheduled) && m.KickoffTime.HasValue)
                .OrderBy(m => m.KickoffTime).Take(4).ToList();
            foreach (var m in next)
            {
                _trending.Add(new("📅", $"{m.HomeTeamShortName} vs {m.AwayTeamShortName} — {m.KickoffTime!.Value:ddd HH:mm}",
                    null, "📅 Coming Up", $"/matches/{m.MatchId}"));
            }
        }

        // Latest results if no live/upcoming
        if (live.Count == 0 && upcoming.Count == 0)
        {
            var results = allMatches
                .Where(m => m.Status == nameof(MatchStatus.Finished))
                .OrderByDescending(m => m.KickoffTime).Take(3).ToList();
            foreach (var m in results)
            {
                _trending.Add(new("✅", $"{m.HomeTeamShortName} {m.HomeScore}-{m.AwayScore} {m.AwayTeamShortName}",
                    null, "✅ Latest Results", $"/matches/{m.MatchId}"));
            }
        }

        // Quick links
        _trending.Add(new("📊", "Points Table", null, "⚡ Quick Links", "/points-table"));
        _trending.Add(new("🏅", "Player Stats", null, "⚡ Quick Links", "/player-stats"));
        _trending.Add(new("🤖", "AI Insights", null, "⚡ Quick Links", "/insights"));
    }

    private async Task Navigate(SearchItem item)
    {
        _query = null;
        _filtered = [];
        await OnClose.InvokeAsync();
        Nav.NavigateTo(item.Url);
    }

    private async Task Close()
    {
        _query = null;
        _filtered = [];
        await OnClose.InvokeAsync();
    }

    public record SearchItem(string Emoji, string Name, string? ImageUrl, string Category, string Url);
}
