using Microsoft.JSInterop;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class PredictionReplay : ScoreCastComponentBase
{
    [Parameter] public long MatchId { get; set; }
    [Parameter] public long UserId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private PredictionReplayResult? _replay;
    private bool _isOwnReplay;

    protected override async Task OnInitializedAsync()
    {
        await Loading.While(async () =>
        {
            try
            {
                var result = await Api.GetPublicPredictionReplayAsync(MatchId, UserId, CancellationToken.None);
                if (result.Success) _replay = result.Data;

                try
                {
                    var profile = await Api.GetMyProfileAsync(CancellationToken.None);
                    if (profile is { Success: true, Data: not null })
                        _isOwnReplay = profile.Data.Id == UserId;
                }
                catch { /* not logged in */ }
            }
            catch { /* endpoint unavailable */ }
        });
    }

    private string PlayerName => _isOwnReplay ? "Your" : $"{_replay?.DisplayName}'s";
    private string PlayerNameUpper => _isOwnReplay ? "YOUR" : $"{_replay?.DisplayName?.ToUpper()}'S";

    private string OutcomeColor(string? outcome) => outcome switch
    {
        "ExactScore" => "#2E7D32",
        "CorrectResultAndGoalDifference" or "CorrectResult" => "#1565C0",
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

    private async Task ShareReplay()
    {
        var baseUrl = Nav.BaseUri.TrimEnd('/');
        var url = $"{baseUrl}/replay/{MatchId}/{UserId}";
        await JS.InvokeVoidAsync("navigator.share", new { title = "My ScoreCast Prediction", url });
    }
}
