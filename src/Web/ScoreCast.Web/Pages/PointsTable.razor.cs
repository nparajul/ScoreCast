using ScoreCast.Models.V1.Responses.Football;
using ScoreCast.Shared.Constants;
using ScoreCast.Shared.Enums;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class PointsTable
{
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private IAlertService Alert { get; set; } = null!;

    private PointsTableResult? _result;
    private BracketResult? _bracket;
    private List<CompetitionZoneResult> _zones = [];
    private CompetitionResult? _selectedCompetition;
    private SeasonResult? _selectedSeason;
    private string _groupTab = "Groups";

    private async Task OnFilterChanged(CompetitionFilterState state)
    {
        _result = null;
        _bracket = null;
        _zones = [];
        _selectedCompetition = state.Competition;
        _selectedSeason = state.Season;

        if (state.Competition is not null)
        {
            var zonesResponse = await Api.GetCompetitionZonesAsync(state.Competition.Code, CancellationToken.None);
            if (zonesResponse is { Success: true, Data: not null })
                _zones = zonesResponse.Data;
        }

        if (state.Season is not null)
            await LoadTableAsync(state.Season.Id);

        StateHasChanged();
    }

    private async Task LoadTableAsync(long seasonId)
    {
        await Loading.While(async () =>
        {
            var tableTask = Api.GetPointsTableAsync(seasonId, CancellationToken.None);
            var bracketTask = Api.GetBracketAsync(seasonId, CancellationToken.None);
            await Task.WhenAll(tableTask, bracketTask);

            var response = await tableTask;
            if (response is { Success: true, Data: not null })
                _result = response.Data;
            else
                Alert.Add("Failed to load points table", Severity.Error);

            var bracketResponse = await bracketTask;
            if (bracketResponse is { Success: true, Data: not null })
                _bracket = bracketResponse.Data;
        });
    }

    private bool IsGroupFormat => _result?.Format is CompetitionFormat.GroupAndKnockout;
}
