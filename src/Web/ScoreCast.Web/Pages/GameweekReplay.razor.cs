using Microsoft.JSInterop;
using ScoreCast.Models.V1.Responses.Prediction;
using ScoreCast.Web.Components;
using ScoreCast.Web.Components.Helpers;

namespace ScoreCast.Web.Pages;

public partial class GameweekReplay : ScoreCastComponentBase
{
    [Parameter] public long SeasonId { get; set; }
    [Parameter] public int GameweekNumber { get; set; }
    [Parameter] public long UserId { get; set; }
    [Inject] private IScoreCastApiClient Api { get; set; } = null!;
    [Inject] private ILoadingService Loading { get; set; } = null!;
    [Inject] private NavigationManager Nav { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private GameweekReplayResult? _data;
    private bool _isOwnReplay;
    private bool _isLoggedIn;

    protected override async Task OnInitializedAsync()
    {
        await Loading.While(async () =>
        {
            try
            {
                var result = await Api.GetGameweekReplayAsync(SeasonId, GameweekNumber, UserId, CancellationToken.None);
                if (result.Success) _data = result.Data;

                try
                {
                    var profile = await Api.GetMyProfileAsync(CancellationToken.None);
                    if (profile is { Success: true, Data: not null })
                    {
                        _isLoggedIn = true;
                        _isOwnReplay = profile.Data.Id == UserId;
                    }
                }
                catch { /* not logged in */ }
            }
            catch { /* endpoint unavailable */ }
        });
    }

    private string PlayerName => _isOwnReplay ? "Your" : $"{_data?.DisplayName}'s";
    private string PlayerNameUpper => _isOwnReplay ? "YOUR" : $"{_data?.DisplayName?.ToUpper()}'S";

    private string OutcomeColor(string? outcome) => outcome switch
    {
        "ExactScore" => "#2E7D32",
        "CorrectResultAndGoalDifference" or "CorrectResult" => "#1565C0",
        "CorrectGoalDifference" => "#FF6B35",
        _ => "#C62828"
    };

    private string OutcomeBg(string? outcome) => outcome switch
    {
        "ExactScore" => "rgba(46,125,50,0.15)",
        "CorrectResultAndGoalDifference" or "CorrectResult" => "rgba(21,101,192,0.1)",
        "CorrectGoalDifference" => "rgba(255,107,53,0.1)",
        _ => "rgba(198,40,40,0.08)"
    };

    private string OutcomeShort(string? outcome) => outcome switch
    {
        "ExactScore" => "🎯 Exact",
        "CorrectResultAndGoalDifference" => "✓ Result+GD",
        "CorrectResult" => "✓ Result",
        "CorrectGoalDifference" => "~ GD",
        _ => "✗"
    };

    private async Task ShareReplay()
    {
        var baseUrl = Nav.BaseUri.TrimEnd('/');
        var url = $"{baseUrl}/gw-replay/{SeasonId}/{GameweekNumber}/{UserId}";
        await JS.InvokeVoidAsync("navigator.share", new { title = $"ScoreCast GW{GameweekNumber} Replay", url });
    }
}
