using Microsoft.AspNetCore.Components;
using ScoreCast.Models.V1.Responses.Football;

namespace ScoreCast.Web.Components.PointsTable;

public partial class MobileRoundView
{
    [Parameter, EditorRequired] public List<BracketSlot> Slots { get; set; } = [];
}
