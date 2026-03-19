using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Teams
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private const int PageSize = 50;
    private string _search = "";
    private List<TeamResult> _teams = [];
    private bool _hasMore;
    private bool _loadingMore;
    private CancellationTokenSource? _searchCts;

    protected override async Task OnInitializedAsync()
    {
        await SearchAsync(reset: true);
    }

    private async Task OnSearchChanged(string value)
    {
        _search = value;
        await SearchAsync(reset: true);
    }

    private async Task SearchAsync(bool reset)
    {
        _searchCts?.Cancel();
        var cts = new CancellationTokenSource();
        _searchCts = cts;

        if (reset)
            _teams = [];

        await Loading.While(async () =>
        {
            var response = await Api.SearchTeamsAsync(_search, _teams.Count, PageSize, cts.Token);
            if (cts.Token.IsCancellationRequested) return;
            if (response is { Success: true, Data: not null })
            {
                if (reset) _teams = [.. response.Data.Teams];
                else _teams.AddRange(response.Data.Teams);
                _hasMore = response.Data.HasMore;
            }
            else
            {
                Alert.Add(response.Message ?? "Failed to search teams", Severity.Error);
            }
        });
    }

    private async Task LoadMoreAsync()
    {
        if (_loadingMore || !_hasMore) return;
        _loadingMore = true;

        var response = await Api.SearchTeamsAsync(_search, _teams.Count, PageSize, CancellationToken.None);
        if (response is { Success: true, Data: not null })
        {
            _teams.AddRange(response.Data.Teams);
            _hasMore = response.Data.HasMore;
        }

        _loadingMore = false;
    }

    private void GoToTeam(long teamId) => Nav.NavigateTo($"/teams/{teamId}");
}
