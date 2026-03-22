using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Pages;

public partial class GlobalPredictions
{
    [Inject] private ApiClient.V1.Apis.IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private List<MatchPredictionSummary> _predictions = [];
    private bool _loaded;

    protected override async Task OnInitializedAsync()
    {
        var result = await Api.GetGlobalDashboardAsync(default);
        if (result is { Success: true, Data: not null })
            _predictions = result.Data.UpcomingPredictions;
        _loaded = true;
    }
}
