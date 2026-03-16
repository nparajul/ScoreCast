using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class LeagueDetail
{
    private const string _appName = "LEAGUE DETAIL";
    [Parameter] public long LeagueId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private LeagueStandingsResult? _standings;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await Loading.While(async () =>
        {
            var response = await Api.GetLeagueStandingsAsync(LeagueId, CancellationToken.None);
            if (response is { Success: true, Data: not null })
                _standings = response.Data;
            else
                Alert.Add(response?.Message ?? "League not found", Severity.Error);
        });

        StateHasChanged();
    }

    private void NavigateToPredict() => Nav.NavigateTo("/predict");
}
