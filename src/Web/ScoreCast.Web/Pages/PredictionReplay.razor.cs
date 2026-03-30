using Microsoft.JSInterop;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class PredictionReplay : ScoreCastComponentBase
{
    [Parameter] public long MatchId { get; set; }
    [Parameter] public long LeagueId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private PredictionReplayResult? _replay;
    private long _internalUserId;

    protected override async Task OnInitializedAsync()
    {
        await Loading.While(async () =>
        {
            var result = await Api.GetPredictionReplayAsync(MatchId, LeagueId, CancellationToken.None);
            if (result.Success) _replay = result.Data;

            var profile = await Api.GetMyProfileAsync(CancellationToken.None);
            if (profile.Success) _internalUserId = profile.Data!.Id;
        });
    }

    private async Task ShareReplay()
    {
        var url = $"https://scorecast.uk/api/v1/share/replay/{MatchId}/{_internalUserId}/og";
        await JS.InvokeVoidAsync("navigator.share", new { title = "My ScoreCast Prediction", url });
    }

    private string OutcomeColor(string? outcome) => outcome switch
    {
        "ExactScore" => "#2E7D32",
        "CorrectResultAndGoalDifference" => "#1565C0",
        "CorrectResult" => "#1565C0",
        "CorrectGoalDifference" => "#FF6B35",
        _ => "#C62828"
    };

    private string OutcomeLabel(string? outcome) => outcome switch
    {
        "ExactScore" => "EXACT SCORE",
        "CorrectResultAndGoalDifference" => "CORRECT RESULT + GD",
        "CorrectResult" => "CORRECT RESULT",
        "CorrectGoalDifference" => "CORRECT GD",
        _ => "INCORRECT"
    };

    private string StatusIcon(string status) => status switch
    {
        "exact" => "✅",
        "alive" => "🟢",
        _ => "💀"
    };

    private string TrendIcon(string trend) => trend switch
    {
        "improving" => "📈",
        "declining" => "📉",
        _ => "➡️"
    };

    private void GoToMatch() => Nav.NavigateTo($"/matches/{MatchId}");
}
