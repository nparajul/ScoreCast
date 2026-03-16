using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Insights
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;

    private List<MatchInsightResult> _insights = [];
    private bool _loaded;

    protected override async Task OnInitializedAsync()
    {
        await Loading.While(async () =>
        {
            // Get default competition to find current season/gameweek
            var compResp = await Api.GetDefaultCompetitionAsync(CancellationToken.None);
            if (compResp is not { Success: true, Data: not null }) return;

            var seasonsResp = await Api.GetSeasonsAsync(compResp.Data.Code, CancellationToken.None);
            var currentSeason = seasonsResp.Data?.FirstOrDefault(s => s.IsCurrent);
            if (currentSeason is null) return;

            // Get current gameweek from matches endpoint
            var gwResp = await Api.GetGameweekMatchesAsync(currentSeason.Id, 0, CancellationToken.None);
            if (gwResp is not { Success: true, Data: not null }) return;

            var resp = await Api.GetMatchInsightsAsync(currentSeason.Id, gwResp.Data.CurrentGameweek, CancellationToken.None);
            if (resp is { Success: true, Data: not null })
                _insights = resp.Data;

            _loaded = true;
        });
    }
}
