using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Scores : ScoreCastComponentBase, IDisposable
{
    private CancellationTokenSource? _pollCts;
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private IClientTimeProvider ClientTime { get; set; } = null!;

    private List<CompetitionSection> _sections = [];
    private readonly HashSet<long> _expandedMatches = [];
    private bool _loaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await ClientTime.InitializeAsync();
        await Loading.While(async () =>
        {
            var comps = await Api.GetCompetitionsAsync(CancellationToken.None);
            if (comps is not { Success: true, Data: not null }) return;

            foreach (var comp in comps.Data)
            {
                var seasons = await Api.GetSeasonsAsync(comp.Code, CancellationToken.None);
                var currentSeason = seasons?.Data?.FirstOrDefault(s => s.IsCurrent);
                _sections.Add(new CompetitionSection
                {
                    Competition = comp,
                    SeasonId = currentSeason?.Id,
                    Expanded = true
                });
            }

            // Load first section expanded by default, rest collapsed
            var loadTasks = _sections.Select(s => LoadSectionAsync(s));
            await Task.WhenAll(loadTasks);
        });

        _loaded = true;
        StateHasChanged();
        StartPolling();
    }

    private async Task LoadSectionAsync(CompetitionSection section)
    {
        if (section.SeasonId is null || section.DataLoaded) return;

        section.Loading = true;
        var gw = await Api.GetGameweekMatchesAsync(section.SeasonId.Value, SharedConstants.CurrentGameweek, CancellationToken.None);
        if (gw is not { Success: true, Data: not null })
        {
            section.Loading = false;
            return;
        }

        section.CurrentGw = gw.Data;
        var today = ClientTime.Today;
        section.TodayMatches = gw.Data.Matches
            .Where(m => m.KickoffTime.HasValue && DateOnly.FromDateTime(ClientTime.ToLocal(m.KickoffTime.Value)) == today)
            .ToList();

        if (section.TodayMatches.Count == 0)
        {
            // Show last GW results
            var lastGwNum = gw.Data.GameweekNumber;
            var hasFinished = gw.Data.Matches.Any(m => m.Status == nameof(MatchStatus.Finished));

            if (hasFinished)
            {
                section.LastGwNumber = lastGwNum;
                section.LastGwMatches = gw.Data.Matches
                    .Where(m => m.Status == nameof(MatchStatus.Finished))
                    .ToList();

                // Load next GW
                if (lastGwNum < gw.Data.TotalGameweeks)
                {
                    var nextGw = await Api.GetGameweekMatchesAsync(section.SeasonId.Value, lastGwNum + 1, CancellationToken.None);
                    if (nextGw is { Success: true, Data: not null })
                    {
                        section.NextGwNumber = lastGwNum + 1;
                        section.NextGwMatches = nextGw.Data.Matches;
                    }
                }
            }
            else
            {
                // Current GW has no finished matches — it IS the next GW
                section.NextGwNumber = lastGwNum;
                section.NextGwMatches = gw.Data.Matches;

                // Load previous GW for results
                if (lastGwNum > 1)
                {
                    var prevGw = await Api.GetGameweekMatchesAsync(section.SeasonId.Value, lastGwNum - 1, CancellationToken.None);
                    if (prevGw is { Success: true, Data: not null })
                    {
                        section.LastGwNumber = lastGwNum - 1;
                        section.LastGwMatches = prevGw.Data.Matches
                            .Where(m => m.Status == nameof(MatchStatus.Finished))
                            .ToList();
                    }
                }
            }
        }

        section.Loading = false;
        section.DataLoaded = true;
    }

    private async Task ToggleSection(CompetitionSection section)
    {
        section.Expanded = !section.Expanded;
        if (section.Expanded && !section.DataLoaded)
        {
            await LoadSectionAsync(section);
            StateHasChanged();
        }
    }

    private void ToggleMatch(long matchId)
    {
        if (!_expandedMatches.Remove(matchId))
            _expandedMatches.Add(matchId);
    }

    private MarkupString FormatDate(DateTime utc)
    {
        var local = ClientTime.ToLocal(utc);
        var date = DateOnly.FromDateTime(local);
        var today = ClientTime.Today;
        var label = date == today ? "Today"
            : date == today.AddDays(1) ? "Tomorrow"
            : date == today.AddDays(-1) ? "Yesterday"
            : date.ToString("dd MMM");
        return new MarkupString($"{label}<br />{local:HH:mm}");
    }

    private void StartPolling()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();

        var liveSections = _sections.Where(s => s.CurrentGw?.Matches.Any(m => m.Status == nameof(MatchStatus.Live)) == true).ToList();
        if (liveSections.Count == 0) return;

        _pollCts = new CancellationTokenSource();
        var token = _pollCts.Token;

        _ = Task.Run(async () =>
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(token))
            {
                foreach (var section in liveSections)
                {
                    if (section.SeasonId is null || section.CurrentGw is null) continue;
                    var response = await Api.GetGameweekMatchesAsync(section.SeasonId.Value, section.CurrentGw.GameweekNumber, CancellationToken.None);
                    if (response is { Success: true, Data: not null })
                    {
                        section.CurrentGw = response.Data;
                        var today = ClientTime.Today;
                        section.TodayMatches = response.Data.Matches
                            .Where(m => m.KickoffTime.HasValue && DateOnly.FromDateTime(ClientTime.ToLocal(m.KickoffTime.Value)) == today)
                            .ToList();
                    }
                }
                await InvokeAsync(StateHasChanged);
            }
        }, token);
    }

    public void Dispose()
    {
        _pollCts?.Cancel();
        _pollCts?.Dispose();
    }

    private class CompetitionSection
    {
        public CompetitionResult Competition { get; init; } = null!;
        public long? SeasonId { get; init; }
        public bool Expanded { get; set; }
        public bool Loading { get; set; }
        public bool DataLoaded { get; set; }
        public GameweekMatchesResult? CurrentGw { get; set; }
        public List<MatchDetail> TodayMatches { get; set; } = [];
        public List<MatchDetail> LastGwMatches { get; set; } = [];
        public List<MatchDetail> NextGwMatches { get; set; } = [];
        public int LastGwNumber { get; set; }
        public int NextGwNumber { get; set; }
    }
}
