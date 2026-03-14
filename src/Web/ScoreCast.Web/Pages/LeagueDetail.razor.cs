using Microsoft.AspNetCore.Components;
using ScoreCast.ApiClient.V1.Apis;
using ScoreCast.Models.V1.Requests.Prediction;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class LeagueDetail
{
    private const string AppName = "LEAGUE DETAIL";
    [Parameter] public long LeagueId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = default!;
    [Inject] private ILoadingService Loading { get; set; } = default!;
    [Inject] private IAlertService Alert { get; set; } = default!;
    [Inject] private NavigationManager Nav { get; set; } = default!;

    private LeagueStandingsResult? _standings;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        await Loading.While(async () =>
        {
            var leaguesResponse = await Api.GetMyLeaguesAsync(CancellationToken.None);
            var league = leaguesResponse?.Data?.FirstOrDefault(l => l.Id == LeagueId);
            if (league is null)
            {
                Alert.Add("League not found", Severity.Error);
                return;
            }

            var result = await Api.CalculateOutcomesAsync(
                new CalculateOutcomesRequest { SeasonId = league.SeasonId },
                CancellationToken.None);
            if (!result.Success)
                Alert.Add(result.Message ?? "Failed to calculate points", Severity.Error);
        }, "Calculating total points...");

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
