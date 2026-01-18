using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Insights : ScoreCastComponentBase
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;

    private List<CompetitionInsights> _groups = [];
    private readonly HashSet<string> _collapsed = [];
    private bool _loaded;

    protected override async Task OnInitializedAsync()
    {
        await Loading.While(async () =>
        {
            var comps = await Api.GetCompetitionsAsync(CancellationToken.None);
            if (comps is not { Success: true, Data: not null }) { _loaded = true; return; }

            var tasks = comps.Data.Select(async comp =>
            {
                var seasons = await Api.GetSeasonsAsync(comp.Code, CancellationToken.None);
                var current = seasons.Data?.FirstOrDefault(s => s.IsCurrent);
                if (current is null) return null;

                var gw = await Api.GetGameweekMatchesAsync(current.Id, 0, CancellationToken.None);
                if (gw is not { Success: true, Data: not null }) return null;

                var resp = await Api.GetMatchInsightsAsync(current.Id, gw.Data.CurrentGameweek, CancellationToken.None);
                if (resp is { Success: true, Data: not null } && resp.Data.Count > 0)
                    return new CompetitionInsights(comp.Name, comp.LogoUrl, resp.Data);

                var next = await Api.GetMatchInsightsAsync(current.Id, gw.Data.CurrentGameweek + 1, CancellationToken.None);
                return next is { Success: true, Data: not null } && next.Data.Count > 0
                    ? new CompetitionInsights(comp.Name, comp.LogoUrl, next.Data)
                    : null;
            });

            var results = await Task.WhenAll(tasks);
            _groups = results.Where(g => g is not null).ToList()!;
            _loaded = true;
        });
    }

    private void ToggleCollapse(string name)
    {
        if (!_collapsed.Remove(name)) _collapsed.Add(name);
    }

    internal record CompetitionInsights(string Name, string? LogoUrl, List<MatchInsightResult> Insights);
}
