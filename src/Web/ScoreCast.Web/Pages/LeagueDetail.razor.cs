using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class LeagueDetail : ScoreCastComponentBase
{
    [Parameter] public long LeagueId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;

    private LeagueStandingsResult? _standings;
    private long _myUserId;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await Loading.While(async () =>
        {
            var standingsTask = Api.GetLeagueStandingsAsync(LeagueId, CancellationToken.None);
            var profileTask = Api.GetMyProfileAsync(CancellationToken.None);
            await Task.WhenAll(standingsTask, profileTask);

            if (standingsTask.Result is { Success: true, Data: not null })
                _standings = standingsTask.Result.Data;
            else
                Alert.Add(standingsTask.Result?.Message ?? "League not found", Severity.Error);

            if (profileTask.Result is { Success: true, Data: not null })
                _myUserId = profileTask.Result.Data.Id;
        });

        StateHasChanged();
    }

    private void NavigateToPlayer(long userId)
    {
        if (userId == _myUserId && _standings is not null)
            Nav.NavigateTo($"/predict/{_standings.SeasonId}");
        else
            Nav.NavigateTo($"/dashboard/{LeagueId}/player/{userId}");
    }

    private void NavigateToPredict() => Nav.NavigateTo("/dashboard");
}
