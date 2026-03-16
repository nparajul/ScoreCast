using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class KnockoutBracket
{
    [Parameter] public BracketResult? Bracket { get; set; }
    [Inject] private IJSRuntime Js { get; set; } = null!;

    private string _mobileRound = "R32";
    private List<string> _mobileRounds = [];
    private double _touchStartX;
    private string _slideClass = "";

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

    private void OnTouchStart(TouchEventArgs e)
    {
        if (e.Touches.Length > 0)
            _touchStartX = e.Touches[0].ClientX;
    }

    private void OnTouchEnd(TouchEventArgs e)
    {
        if (e.ChangedTouches.Length == 0 || _mobileRounds.Count == 0) return;

        var deltaX = e.ChangedTouches[0].ClientX - _touchStartX;
        if (Math.Abs(deltaX) < 50) return;

        var idx = _mobileRounds.IndexOf(_mobileRound);
        if (deltaX < 0 && idx < _mobileRounds.Count - 1)
        {
            _slideClass = "slide-from-right";
            _mobileRound = _mobileRounds[idx + 1];
        }
        else if (deltaX > 0 && idx > 0)
        {
            _slideClass = "slide-from-left";
            _mobileRound = _mobileRounds[idx - 1];
        }
    }

    private void SetMobileRound(string tab)
    {
        if (tab == _mobileRound) return;
        var oldIdx = _mobileRounds.IndexOf(_mobileRound);
        var newIdx = _mobileRounds.IndexOf(tab);
        _slideClass = newIdx > oldIdx ? "slide-from-right" : "slide-from-left";
        _mobileRound = tab;
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

    private static readonly string[] _roundOrder = ["Round of 32", "Round of 16", "Quarter-Finals", "Semi-Finals", "Final"];

    private static List<BracketSlot>? GetNextRound(Dictionary<string, BracketRound> roundMap, string currentLabel)
    {
        var currentFull = _roundOrder.FirstOrDefault(r => GetRoundLabel(r) == currentLabel);
        if (currentFull is null) return null;
        var idx = Array.IndexOf(_roundOrder, currentFull);
        if (idx < 0 || idx + 1 >= _roundOrder.Length) return null;
        return roundMap.TryGetValue(_roundOrder[idx + 1], out var next) && next.Slots.Count > 0 ? next.Slots : null;
    }
}
