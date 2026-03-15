using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class KnockoutBracket
{
    [Parameter] public BracketResult? Bracket { get; set; }
    [Inject] private IJSRuntime Js { get; set; } = default!;

    private int MobileRoundIndex { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (Bracket is not null && Bracket.Rounds.Count > 0)
        {
            await Task.Delay(200); // let DOM settle
            await Js.InvokeVoidAsync("bracketLines.draw", "bracket-container");
        }
    }
}
