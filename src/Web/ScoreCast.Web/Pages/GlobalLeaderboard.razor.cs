using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Pages;

public partial class GlobalLeaderboard
{
    [Inject] private ApiClient.V1.Apis.IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private List<GlobalLeaderboardEntry> _entries = [];
    private bool _loaded;

    protected override async Task OnInitializedAsync()
    {
        var result = await Api.GetGlobalLeaderboardAsync(null, default);
        if (result is { Success: true, Data: not null })
            _entries = result.Data.Entries;
        _loaded = true;
    }
}
