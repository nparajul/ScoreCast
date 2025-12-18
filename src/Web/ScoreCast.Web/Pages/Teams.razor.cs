using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class Teams
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private string _search = "";
    private List<TeamResult> _teams = [];

    protected override async Task OnInitializedAsync()
    {
        await SearchAsync();
    }

    private async Task OnSearchChanged(string value)
    {
        _search = value;
        await SearchAsync();
    }

    private async Task SearchAsync()
    {
        await Loading.While(async () =>
        {
            var response = await Api.SearchTeamsAsync(_search, CancellationToken.None);
            if (response is { Success: true, Data: not null })
                _teams = response.Data.Teams;
            else
                Alert.Add(response.Message ?? "Failed to search teams", Severity.Error);
        });
    }

    private void GoToTeam(long teamId) => Nav.NavigateTo($"/teams/{teamId}");
}
