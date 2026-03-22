using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Shared.Types;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Insights : ScoreCastComponentBase
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IClientTimeProvider ClientTime { get; set; } = null!;

    private List<CompetitionInsights> _groups = [];
    private readonly HashSet<string> _collapsed = [];
    private bool _loaded;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await ClientTime.InitializeAsync();
        await Loading.While(async () =>
        {
            var defaultCompTask = Api.GetDefaultCompetitionAsync(CancellationToken.None);
            var compsTask = Api.GetCompetitionsAsync(CancellationToken.None);
            await Task.WhenAll(defaultCompTask, compsTask);

            if (compsTask.Result is not { Success: true, Data: not null }) { _loaded = true; StateHasChanged(); return; }

            var defaultCode = defaultCompTask.Result is { Success: true, Data: not null }
                ? defaultCompTask.Result.Data.Code : "PL";

            var defaultComp = compsTask.Result.Data.FirstOrDefault(c => c.Code == defaultCode);
            var otherComps = compsTask.Result.Data.Where(c => c.Code != defaultCode).ToList();

            var compsToLoad = new List<CompetitionResult>();
            if (defaultComp is not null) compsToLoad.Add(defaultComp);

            var cutoff = ScoreCastDateTime.Now.Value.AddDays(7);
            foreach (var comp in otherComps)
            {
                var season = (await Api.GetSeasonsAsync(comp.Code, CancellationToken.None))?.Data?.FirstOrDefault(s => s.IsCurrent);
                if (season is null) continue;

                var gw = await Api.GetGameweekMatchesAsync(season.Id, SharedConstants.CurrentGameweek, CancellationToken.None);
                if (gw is not { Success: true, Data: not null }) continue;

                var hasRelevant = gw.Data.Matches.Any(m =>
                    m.Status == nameof(MatchStatus.Live) ||
                    m.Status == nameof(MatchStatus.Finished) ||
                    (m.KickoffTime.HasValue && m.KickoffTime.Value <= cutoff));

                if (hasRelevant) compsToLoad.Add(comp);
            }

            var now = ScoreCastDateTime.Now.Value;
            var tasks = compsToLoad.Select(async comp =>
            {
                var seasons = await Api.GetSeasonsAsync(comp.Code, CancellationToken.None);
                var current = seasons.Data?.FirstOrDefault(s => s.IsCurrent);
                if (current is null) return null;

                var gw = await Api.GetGameweekMatchesAsync(current.Id, 0, CancellationToken.None);
                if (gw is not { Success: true, Data: not null }) return null;

                var excludedIds = gw.Data.Matches
                    .Where(m => m.Status is nameof(MatchStatus.Postponed) or nameof(MatchStatus.Live) or nameof(MatchStatus.Finished))
                    .Select(m => m.MatchId).ToHashSet();

                List<MatchInsightResult>? Filter(List<MatchInsightResult>? list) =>
                    list?.Where(m => m.KickoffTime.HasValue && m.KickoffTime.Value > now && !excludedIds.Contains(m.MatchId))
                        .ToList() is { Count: > 0 } f ? f : null;

                var resp = await Api.GetMatchInsightsAsync(current.Id, gw.Data.CurrentGameweek, CancellationToken.None);
                var filtered = resp is { Success: true, Data: not null } ? Filter(resp.Data) : null;
                if (filtered is not null)
                    return new CompetitionInsights(comp.Name, comp.LogoUrl, comp.CountryFlagUrl, filtered);

                var next = await Api.GetMatchInsightsAsync(current.Id, gw.Data.CurrentGameweek + 1, CancellationToken.None);
                var nextFiltered = next is { Success: true, Data: not null } ? Filter(next.Data) : null;
                return nextFiltered is not null
                    ? new CompetitionInsights(comp.Name, comp.LogoUrl, comp.CountryFlagUrl, nextFiltered)
                    : null;
            });

            var results = await Task.WhenAll(tasks);
            _groups = results.Where(g => g is not null).ToList()!;
            _loaded = true;
        });

        StateHasChanged();
    }

    private void ToggleCollapse(string name)
    {
        if (!_collapsed.Remove(name)) _collapsed.Add(name);
    }

    internal record CompetitionInsights(string Name, string? LogoUrl, string? CountryFlagUrl, List<MatchInsightResult> Insights);
}
