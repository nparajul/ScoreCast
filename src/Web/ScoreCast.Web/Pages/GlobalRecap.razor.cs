using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Pages;

public partial class GlobalRecap
{
    [Inject] private ApiClient.V1.Apis.IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private GameweekRecap? _recap;
    private bool _loaded;

    protected override async Task OnInitializedAsync()
    {
        var result = await Api.GetGlobalDashboardAsync(null, default);
        if (result is { Success: true, Data.LastGameweekRecap: not null })
            _recap = result.Data.LastGameweekRecap;
        _loaded = true;
    }
}
