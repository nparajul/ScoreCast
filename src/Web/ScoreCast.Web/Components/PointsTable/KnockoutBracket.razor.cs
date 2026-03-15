using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class KnockoutBracket
{
    [Parameter] public BracketResult? Bracket { get; set; }
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private string _mobileRound = "R32";
    private List<string> _mobileRounds = [];

    protected override void OnParametersSet()
    {
        if (Bracket is null) return;

        var roundMap = Bracket.Rounds.ToDictionary(r => r.Name);
        _mobileRounds = [];

        foreach (var name in new[] { "Round of 32", "Round of 16", "Quarter-Finals", "Semi-Finals" })
        {
            if (roundMap.TryGetValue(name, out var r) && r.Slots.Count > 0)
                _mobileRounds.Add(GetRoundLabel(name));
        }

        if (roundMap.ContainsKey("Final") || roundMap.ContainsKey("Third Place"))
            _mobileRounds.Add("Final");

        if (_mobileRounds.Count > 0 && !_mobileRounds.Contains(_mobileRound))
            _mobileRound = _mobileRounds[0];
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Bracket is not null && Bracket.Rounds.Count > 0)
        {
            await Task.Delay(200);
            await Js.InvokeVoidAsync("bracketLines.draw", "bracket-container");
        }
    }

    private static string GetRoundLabel(string roundName) => roundName switch
    {
        "Round of 32" => "R32",
        "Round of 16" => "R16",
        "Quarter-Finals" => "QF",
        "Semi-Finals" => "SF",
        "Final" => "Final",
        "Third Place" => "Final",
        _ => roundName
    };
}
